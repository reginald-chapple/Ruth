using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.Modules.Finance.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Organizations;

internal sealed class CreateOrganizationCommandHandler(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<CreateOrganizationCommand, OrganizationDetailResponse>
{
    public async Task<Result<OrganizationDetailResponse>> HandleAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 160)
        {
            return FinanceErrors.OrganizationNameInvalid;
        }

        var cleanName = command.Name.Trim();
        PhoneNumberInputDto[]? phones = string.IsNullOrWhiteSpace(command.PhoneNumber)
            ? null
            : [new PhoneNumberInputDto(PhoneNumberType.Work, command.PhoneNumber.Trim())];

        var crmCommand = new CreatePersonCommand(
            FirstName: cleanName,
            LastName: cleanName,
            NickName: cleanName,
            RecordType: PersonRecordType.Business,
            RecordStatus: PersonRecordStatus.Active,
            ConnectionStatus: PersonConnectionStatus.Participant,
            Email: string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim(),
            PrimaryBranchId: command.PrimaryBranchId,
            PhoneNumbers: phones,
            Attributes: OrganizationCrmAttributeKeys.BuildAttributeDictionary(
                command.TaxIdentifier,
                command.SponsorTier,
                command.WebsiteUrl,
                command.BillingNotes
            )
        );

        var created = await commandDispatcher.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            crmCommand,
            cancellationToken
        );
        return created.IsFailure
            ? Result.Failure<OrganizationDetailResponse>(created.Errors)
            : await queryDispatcher.QueryAsync<
                GetOrganizationByIdQuery,
                OrganizationDetailResponse
            >(new GetOrganizationByIdQuery(created.Value.Id), cancellationToken);
    }
}
