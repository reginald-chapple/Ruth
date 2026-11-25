using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Verification;

/// <summary>Command to issue a new identity verification challenge with daily IP throttling.</summary>
public sealed record IssueIdentityVerificationCommand(
    string ReferenceNumber,
    string RequestIpAddress,
    int DailyIpLimit = 20
) : ICommand<IdentityVerificationIssuedResponse>;
