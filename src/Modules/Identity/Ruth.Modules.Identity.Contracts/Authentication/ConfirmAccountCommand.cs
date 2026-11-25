using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Confirms a user account using a single-use confirmation token.</summary>
public sealed record ConfirmAccountCommand(string Token) : ICommand<bool>;
