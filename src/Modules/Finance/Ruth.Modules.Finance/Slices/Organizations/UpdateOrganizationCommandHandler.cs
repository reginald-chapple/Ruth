using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.Modules.Finance.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Organizations;

internal sealed class UpdateOrganizationCommandHandler(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<UpdateOrganizationCommand, OrganizationDetailResponse>
{
    public async Task<Result<OrganizationDetailResponse>> HandleAsync(
        UpdateOrganizationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 160)
        {
            return FinanceErrors.OrganizationNameInvalid;
        }

        var existing = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(command.OrganizationPersonId),
            cancellationToken
        );
        if (existing.IsFailure || existing.Value.RecordType != PersonRecordType.Business)
        {
            return FinanceErrors.OrganizationNotFound;
        }

        var cleanName = command.Name.Trim();
        var personUpdated = await commandDispatcher.SendAsync<
            UpdatePersonCommand,
            PersonDetailResponse
        >(
            new UpdatePersonCommand(
                PersonId: existing.Value.Id,
                FirstName: cleanName,
                LastName: cleanName,
                NickName: cleanName,
                RecordStatus: command.IsActive
                    ? PersonRecordStatus.Active
                    : PersonRecordStatus.Inactive,
                ConnectionStatus: existing.Value.ConnectionStatus,
                Email: string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim(),
                PrimaryBranchId: command.PrimaryBranchId
            ),
            cancellationToken
        );
        if (personUpdated.IsFailure)
        {
            return Result.Failure<OrganizationDetailResponse>(personUpdated.Errors);
        }

        var attrResult = await ApplyOrganizationAttributesAsync(
            existing.Value,
            command,
            cancellationToken
        );
        return attrResult.IsFailure
            ? Result.Failure<OrganizationDetailResponse>(attrResult.Errors)
            : await queryDispatcher.QueryAsync<
                GetOrganizationByIdQuery,
                OrganizationDetailResponse
            >(new GetOrganizationByIdQuery(existing.Value.Id), cancellationToken);
    }

    private async Task<Result<PersonDetailResponse>> ApplyOrganizationAttributesAsync(
        PersonDetailResponse existing,
        UpdateOrganizationCommand command,
        CancellationToken cancellationToken
    )
    {
        var merged = new Dictionary<string, string?>(
            existing.Attributes,
            StringComparer.OrdinalIgnoreCase
        );
        foreach (
            var pair in OrganizationCrmAttributeKeys.BuildAttributeDictionary(
                command.TaxIdentifier,
                command.SponsorTier,
                command.WebsiteUrl,
                command.BillingNotes
            )
        )
        {
            merged[pair.Key] = pair.Value;
        }

        return await commandDispatcher.SendAsync<
            UpdatePersonAttributesCommand,
            PersonDetailResponse
        >(new UpdatePersonAttributesCommand(existing.Id, merged), cancellationToken);
    }
}
