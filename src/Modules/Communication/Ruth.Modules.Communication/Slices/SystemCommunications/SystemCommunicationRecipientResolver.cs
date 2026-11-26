using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class SystemCommunicationRecipientResolver(
    IQueryHandler<GetPersonByIdQuery, PersonDetailResponse>? personQueryHandler = null
)
{
    public async Task<(string? Email, string? Name, string? Phone)> ResolveRecipientAsync(
        SendSystemCommunicationCommand command,
        CancellationToken cancellationToken
    )
    {
        var email = Clean(command.RecipientEmail);
        var name = Clean(command.RecipientName);
        var phone = Clean(command.RecipientPhoneNumber);

        if (
            personQueryHandler is null
            || !command.RecipientPersonId.HasValue
            || command.RecipientPersonId.Value == Guid.Empty
            || (email is not null && name is not null && phone is not null)
        )
        {
            return (email, name, phone);
        }

        var personResult = await personQueryHandler.HandleAsync(
            new GetPersonByIdQuery(command.RecipientPersonId.Value),
            cancellationToken
        );
        if (personResult.IsFailure)
        {
            return (email, name, phone);
        }

        var person = personResult.Value;
        email ??=
            person.IsEmailActive && person.EmailPreference != EmailPreference.DoNotEmail
                ? Clean(person.Email)
                : null;
        name ??= Clean(person.FullName);

        if (phone is null)
        {
            var smsPhone = person.PhoneNumbers.FirstOrDefault(static p =>
                p.IsMessagingEnabled && !p.IsMessagingOptedOut
            );
            phone = Clean(smsPhone?.E164Number);
        }

        return (email, name, phone);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
