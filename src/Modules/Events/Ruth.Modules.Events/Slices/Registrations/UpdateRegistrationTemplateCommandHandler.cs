using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class UpdateRegistrationTemplateCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<UpdateRegistrationTemplateCommand, RegistrationTemplateResponse>
{
    public async Task<Result<RegistrationTemplateResponse>> HandleAsync(
        UpdateRegistrationTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = await loader.LoadTemplateAsync(
            RegistrationTemplateId.From(command.TemplateId),
            asNoTracking: false,
            cancellationToken
        );
        if (template is null)
        {
            return EventsRegistrationErrors.TemplateNotFound;
        }

        var accRes = await queryDispatcher.QueryAsync<
            GetFinancialAccountByIdQuery,
            FinancialAccountResponse
        >(new GetFinancialAccountByIdQuery(command.Spec.FinancialAccountId), cancellationToken);
        if (accRes.IsFailure)
        {
            return Result.Failure<RegistrationTemplateResponse>(accRes.Errors);
        }

        var updated = template.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<RegistrationTemplateResponse>(updated.Errors);
        }

        foreach (var form in template.Forms)
        {
            _ = dbContext.RegistrationTemplateForms.Add(form);
            foreach (var fld in form.Fields)
            {
                _ = dbContext.RegistrationTemplateFormFields.Add(fld);
            }
        }

        foreach (var fee in template.Fees)
        {
            _ = dbContext.RegistrationTemplateFees.Add(fee);
            foreach (var item in fee.Items)
            {
                _ = dbContext.RegistrationTemplateFeeItems.Add(item);
            }
        }

        foreach (var disc in template.Discounts)
        {
            _ = dbContext.RegistrationTemplateDiscounts.Add(disc);
        }

        foreach (var plc in template.GroupPlacements)
        {
            _ = dbContext.RegistrationTemplatePlacements.Add(plc);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return RegistrationMapper.MapTemplate(template);
    }
}
