using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Domain.WalletPasses;

namespace Ruth.Modules.CheckIn.Infrastructure;

internal sealed partial class CheckInBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CheckInBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CheckInDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedSystemLabelsAsync(dbContext, cancellationToken);
        await SeedDefaultWalletPassTemplateAsync(dbContext, cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task SeedSystemLabelsAsync(
        CheckInDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var existingKeys = await dbContext
            .CheckInLabels.AsNoTracking()
            .Select(static l => l.Key)
            .ToListAsync(cancellationToken);
        var keySet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        CheckInLabelSpec[] defaults =
        [
            new(
                "Standard Participant Safety Badge",
                "participant-safety-badge",
                CheckInLabelKind.ParticipantBadge,
                CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto(),
                "Default 4x2 thermal participant name, room, security code, and medical alert badge.",
                ConfigurationId: null,
                AreaId: null,
                CustomZplOverride: null,
                PrintOrder: 0,
                Copies: 1,
                IsActive: true,
                IsSystem: true
            ),
            new(
                "Standard Guardian Pickup Receipt",
                "guardian-pickup-receipt",
                CheckInLabelKind.GuardianPickupReceipt,
                CheckInLabelLayoutSchema.CreateDefaultGuardianReceiptDto(),
                "Default 4x2 thermal guardian security pickup claim receipt with Code 128 barcode.",
                ConfigurationId: null,
                AreaId: null,
                CustomZplOverride: null,
                PrintOrder: 10,
                Copies: 1,
                IsActive: true,
                IsSystem: true
            ),
        ];

        var added = false;
        foreach (var spec in defaults)
        {
            if (keySet.Contains(spec.Key))
            {
                continue;
            }

            var created = CheckInLabel.Create(spec);
            if (created.IsSuccess)
            {
                _ = dbContext.CheckInLabels.Add(created.Value);
                added = true;
            }
        }

        if (added)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedDefaultWalletPassTemplateAsync(
        CheckInDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var exists = await dbContext.WalletPassTemplates.AnyAsync(
            static t => t.Key == "member-access-pass",
            cancellationToken
        );
        if (exists)
        {
            return;
        }

        var created = WalletPassTemplate.Create(
            new WalletPassTemplateSpec(
                "Standard Member & Access Pass",
                "member-access-pass",
                "pass.org.ruth.member",
                "Ruth Community",
                "Digital member credential, punch balance, and Kiosk express check-in pass.",
                WalletPassCardStyle.MemberCredential,
                BackgroundColorHex: "#1E3A8A",
                ForegroundColorHex: "#FFFFFF",
                LabelColorHex: "#BFDBFE",
                LogoText: "Ruth Pass",
                DefaultValidityDays: 365,
                IsActive: true,
                IsSystem: true,
                SystemGuid: CheckInSystemGuids.SeededMemberWalletPassTemplate
            )
        );
        if (created.IsSuccess)
        {
            created.Value.SetCreated(DateTimeOffset.UtcNow);
            _ = dbContext.WalletPassTemplates.Add(created.Value);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    [LoggerMessage(
        EventId = 5301,
        Level = LogLevel.Information,
        Message = "Applying CheckIn schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5302,
        Level = LogLevel.Information,
        Message = "CheckIn schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
