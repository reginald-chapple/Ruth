namespace Ruth.Modules.Tenancy.Contracts.Operators;

/// <summary>Authenticated Platform Operator identity returned upon valid operator credentials.</summary>
/// <param name="OperatorId">The platform operator identifier.</param>
/// <param name="Email">The normalized operator email address.</param>
/// <param name="DisplayName">The operator display name.</param>
public sealed record PlatformOperatorAuthResponse(
    Guid OperatorId,
    string Email,
    string DisplayName
);
