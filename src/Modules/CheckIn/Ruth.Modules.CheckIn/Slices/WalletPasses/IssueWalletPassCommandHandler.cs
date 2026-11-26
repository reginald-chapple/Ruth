using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class IssueWalletPassCommandHandler(
    CheckInDbContext dbContext,
    WalletPassBundleComposer bundleComposer,
    IClock clock,
    IQueryHandler<GetPersonByIdQuery, PersonDetailResponse>? personQueryHandler = null
) : ICommandHandler<IssueWalletPassCommand, IssueWalletPassResultResponse>
{
    public async Task<Result<IssueWalletPassResultResponse>> HandleAsync(
        IssueWalletPassCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var tid = WalletPassTemplateId.From(command.TemplateId);
        var template = await dbContext
            .WalletPassTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tid, cancellationToken);
        if (template is null)
        {
            return Result.Failure<IssueWalletPassResultResponse>(WalletPassErrors.TemplateNotFound);
        }

        var (hydratedFields, resolvedEmail, resolvedPhone) = await HydrateHolderAsync(
            command,
            cancellationToken
        );
        var nowUtc = clock.UtcNow;
        var issuedResult = IssuedWalletPass.Issue(
            template,
            command.PersonId,
            hydratedFields,
            bundleComposer.SecurityOptions.PassKitHmacSecret,
            nowUtc,
            command.ExpiresAtUtc
        );
        if (issuedResult.IsFailure)
        {
            return Result.Failure<IssueWalletPassResultResponse>(issuedResult.Errors);
        }

        var (pass, rawClaimToken) = issuedResult.Value;
        _ = dbContext.IssuedWalletPasses.Add(pass);

        var claimUrl = BuildClaimUrl(command.PublicBaseUrl, rawClaimToken);
        var queued = EnqueueClaimNotificationIfRequested(
            command.SendClaimNotification,
            pass,
            template,
            claimUrl,
            resolvedEmail,
            resolvedPhone,
            nowUtc
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new IssueWalletPassResultResponse(
            WalletPassMapper.ToResponse(pass, template.Name, registeredDeviceCount: 0),
            rawClaimToken,
            claimUrl,
            queued,
            bundleComposer.GetProviderStatuses()
        );
    }

    private async Task<(
        WalletPassDynamicFieldsDto Fields,
        string? Email,
        string? Phone
    )> HydrateHolderAsync(IssueWalletPassCommand command, CancellationToken cancellationToken)
    {
        var fields = command.DynamicFields ?? new WalletPassDynamicFieldsDto();
        var email = command.RecipientEmail;
        var phone = command.RecipientPhoneNumber;

        if (command.PersonId == Guid.Empty || personQueryHandler is null)
        {
            return (fields, email, phone);
        }

        var personResult = await personQueryHandler.HandleAsync(
            new GetPersonByIdQuery(command.PersonId),
            cancellationToken
        );
        if (personResult.IsFailure)
        {
            return (fields, email, phone);
        }

        var p = personResult.Value;
        var sms = p
            .PhoneNumbers.FirstOrDefault(static n => n.IsMessagingEnabled && !n.IsMessagingOptedOut)
            ?.E164Number;
        return (
            fields with
            {
                HolderDisplayName = fields.HolderDisplayName ?? p.FullName,
            },
            email ?? (p.IsEmailActive ? p.Email : null),
            phone ?? sms
        );
    }

    private string BuildClaimUrl(string? overrideBaseUrl, string rawClaimToken)
    {
        var baseUrl = (
            string.IsNullOrWhiteSpace(overrideBaseUrl)
                ? bundleComposer.SecurityOptions.PublicBaseUrl
                : overrideBaseUrl
        ).TrimEnd('/');
        return $"{baseUrl}/api/checkin/wallet-passes/claim/{rawClaimToken}";
    }

    private bool EnqueueClaimNotificationIfRequested(
        bool requested,
        IssuedWalletPass pass,
        WalletPassTemplate template,
        string claimUrl,
        string? email,
        string? phone,
        DateTimeOffset nowUtc
    )
    {
        if (!requested)
        {
            return false;
        }

        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PassName"] = template.Name,
            ["OrganizationName"] = template.OrganizationName,
            ["SerialNumber"] = pass.SerialNumber,
            ["ScanToken"] = pass.ScanToken,
            ["ClaimUrl"] = claimUrl,
            ["MembershipTier"] = pass.DynamicFields.MembershipTier ?? "Member",
        };

        dbContext.EnqueueIntegrationEvent(
            new SystemNotificationRequestedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc.ToUniversalTime(),
                "checkin.wallet-pass-claim",
                RecipientEmail: email,
                RecipientName: pass.DynamicFields.HolderDisplayName,
                RecipientPhoneNumber: phone,
                RecipientPersonId: pass.PersonId,
                MergeFields: merge,
                TriggerModule: "CheckIn.WalletPasses"
            )
        );
        return true;
    }
}
