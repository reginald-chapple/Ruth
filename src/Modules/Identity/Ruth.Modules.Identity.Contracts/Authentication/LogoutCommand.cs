using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Revokes an active refresh token to end an API/mobile session.</summary>
public sealed record LogoutCommand(string RefreshToken) : ICommand<bool>;
