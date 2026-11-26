using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class CreateRegistrationTemplateCommandHandler(
    EventsDbContext dbContext,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<CreateRegistrationTemplateCommand, RegistrationTemplateResponse>
{
    public async Task<Result<RegistrationTemplateResponse>> HandleAsync(
        CreateRegistrationTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = RegistrationTemplate.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<RegistrationTemplateResponse>(created.Errors);
        }

        var accRes = await queryDispatcher.QueryAsync<
            GetFinancialAccountByIdQuery,
            FinancialAccountResponse
        >(new GetFinancialAccountByIdQuery(created.Value.FinancialAccountId), cancellationToken);
        if (accRes.IsFailure)
        {
            return Result.Failure<RegistrationTemplateResponse>(accRes.Errors);
        }

        _ = dbContext.RegistrationTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return RegistrationMapper.MapTemplate(created.Value);
    }
}
