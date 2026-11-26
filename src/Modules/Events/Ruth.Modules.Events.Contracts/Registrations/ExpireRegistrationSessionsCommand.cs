using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to expire stale TTL seat-hold sessions and auto-promote waitlisted registrants into freed seats.</summary>
public sealed record ExpireRegistrationSessionsCommand
    : ICommand<RegistrationSessionCleanupResultResponse>;
