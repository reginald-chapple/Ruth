using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.PhoneNumbers;

/// <summary>Deletes a SystemPhoneNumber when not referenced by active communications.</summary>
/// <param name="SystemPhoneNumberId">Target system phone number identifier.</param>
public sealed record DeleteSystemPhoneNumberCommand(Guid SystemPhoneNumberId) : ICommand<bool>;
