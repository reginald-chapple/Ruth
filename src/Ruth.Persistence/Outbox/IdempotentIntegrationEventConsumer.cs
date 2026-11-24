using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Outbox;

/// <summary>
/// Wraps an <see cref="IIntegrationEventConsumer{TEvent}" /> with per-module-schema
/// <see cref="InboxMessage" /> (MessageId, ConsumerName) deduplication.
/// </summary>
public sealed class IdempotentIntegrationEventConsumer<TEvent, TConsumer, TDbContext>(
    TConsumer inner,
    TDbContext dbContext,
    IClock clock,
    ILogger<IdempotentIntegrationEventConsumer<TEvent, TConsumer, TDbContext>> logger
) : IIntegrationEventConsumer<TEvent>
    where TEvent : IIntegrationEvent
    where TConsumer : class, IIntegrationEventConsumer<TEvent>
    where TDbContext : ModuleDbContext
{
    private static readonly string _consumerName = BuildConsumerName();

    private readonly TConsumer _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    private readonly TDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));

    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    private readonly ILogger<
        IdempotentIntegrationEventConsumer<TEvent, TConsumer, TDbContext>
    > _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task ConsumeAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (integrationEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Inbox message ID cannot be empty.",
                nameof(integrationEvent)
            );
        }

        await ConsumeRelationalAsync(integrationEvent, cancellationToken);
    }

    private async Task ConsumeRelationalAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken
    )
    {
        var existingTransaction = _dbContext.Database.CurrentTransaction;
        await using var ownedTransaction =
            existingTransaction is null
                ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
                : null;
        var activeTransaction = ownedTransaction ?? existingTransaction!;
        var savepointName = await CreateSavepointIfJoinedAsync(
            activeTransaction,
            ownedTransaction is null,
            cancellationToken
        );

        try
        {
            var claimed = await TryClaimInboxRelationalAsync(
                activeTransaction,
                integrationEvent.EventId,
                cancellationToken
            );

            if (!claimed)
            {
                await ReleaseSavepointIfPresentAsync(
                    activeTransaction,
                    savepointName,
                    cancellationToken
                );
                OutboxLog.DuplicateSkipped(_logger, _consumerName, integrationEvent.EventId);
                return;
            }

            await _inner.ConsumeAsync(integrationEvent, cancellationToken);

            if (_dbContext.ChangeTracker.HasChanges())
            {
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await CompleteRelationalTransactionAsync(
                ownedTransaction,
                activeTransaction,
                savepointName,
                cancellationToken
            );
        }
        catch
        {
            await RollbackRelationalAsync(ownedTransaction, activeTransaction, savepointName);
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<bool> TryClaimInboxRelationalAsync(
        IDbContextTransaction transaction,
        Guid messageId,
        CancellationToken cancellationToken
    )
    {
        var schema = SchemaValidator.Validate(_dbContext.ModuleSchema);
        var connection = _dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = BuildClaimSql(schema);

        AddParameter(command, "message_id", messageId);
        AddParameter(command, "consumer_name", _consumerName);
        AddParameter(command, "processed_on_utc", _clock.UtcNow.ToUniversalTime());

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    private static async Task<string?> CreateSavepointIfJoinedAsync(
        IDbContextTransaction transaction,
        bool isJoinedTransaction,
        CancellationToken cancellationToken
    )
    {
        if (!isJoinedTransaction || !transaction.SupportsSavepoints)
        {
            return null;
        }

        var savepointName = $"inbox_{Guid.NewGuid():N}";
        await transaction.CreateSavepointAsync(savepointName, cancellationToken);
        return savepointName;
    }

    private static async Task ReleaseSavepointIfPresentAsync(
        IDbContextTransaction transaction,
        string? savepointName,
        CancellationToken cancellationToken
    )
    {
        if (savepointName is not null)
        {
            await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
        }
    }

    private static async Task CompleteRelationalTransactionAsync(
        IDbContextTransaction? ownedTransaction,
        IDbContextTransaction activeTransaction,
        string? savepointName,
        CancellationToken cancellationToken
    )
    {
        if (ownedTransaction is not null)
        {
            await ownedTransaction.CommitAsync(cancellationToken);
            return;
        }

        await ReleaseSavepointIfPresentAsync(activeTransaction, savepointName, cancellationToken);
    }

    private async Task RollbackRelationalAsync(
        IDbContextTransaction? ownedTransaction,
        IDbContextTransaction activeTransaction,
        string? savepointName
    )
    {
        if (_dbContext.Database.GetDbConnection().State != ConnectionState.Open)
        {
            return;
        }

        if (ownedTransaction is not null)
        {
            await ownedTransaction.RollbackAsync(CancellationToken.None);
            return;
        }

        if (savepointName is not null)
        {
            await activeTransaction.RollbackToSavepointAsync(savepointName, CancellationToken.None);
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        _ = command.Parameters.Add(parameter);
    }

    private static string BuildClaimSql(string schema)
    {
        return string.Join(
            '\n',
            $"INSERT INTO \"{schema}\".\"{OutboxSqlConstants.InboxTable}\" (",
            $"    {OutboxSqlConstants.ColumnMessageId},",
            $"    {OutboxSqlConstants.ColumnConsumerName},",
            $"    {OutboxSqlConstants.ColumnProcessedOnUtc}",
            ")",
            "VALUES (@message_id, @consumer_name, @processed_on_utc)",
            $"ON CONFLICT ({OutboxSqlConstants.ColumnMessageId}, {OutboxSqlConstants.ColumnConsumerName}) DO NOTHING;"
        );
    }

    private static string BuildConsumerName()
    {
        const int maxLength = 256;
        var fullName = typeof(TConsumer).FullName ?? typeof(TConsumer).Name;
        return fullName.Length <= maxLength ? fullName : fullName[..maxLength];
    }
}
