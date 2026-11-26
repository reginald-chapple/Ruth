namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Projection of an SmsPipeline keyword responder configuration and its ordered actions.</summary>
/// <param name="Id">Unique pipeline identifier.</param>
/// <param name="Name">Pipeline name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="SystemPhoneNumberId">Optional specific system phone number scope (null applies to all numbers).</param>
/// <param name="PriorityOrder">Zero-based pipeline evaluation priority.</param>
/// <param name="IsSystem">Whether built-in and protected from deletion.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="Actions">Ordered keyword actions.</param>
/// <param name="CreatedOnUtc">Creation timestamp (UTC).</param>
public sealed record SmsPipelineResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? SystemPhoneNumberId,
    int PriorityOrder,
    bool IsSystem,
    bool IsActive,
    IReadOnlyList<SmsActionResponse> Actions,
    DateTimeOffset CreatedOnUtc
);
