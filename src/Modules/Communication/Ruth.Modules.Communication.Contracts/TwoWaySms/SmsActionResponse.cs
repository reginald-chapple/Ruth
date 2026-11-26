namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Projection of an SmsAction rule within an SmsPipeline.</summary>
/// <param name="Id">Unique action identifier.</param>
/// <param name="Name">Action title.</param>
/// <param name="Order">Execution order.</param>
/// <param name="MatchMode">Keyword matching strategy.</param>
/// <param name="Keyword">Configured keyword token.</param>
/// <param name="ActionKind">Automated behavior kind.</param>
/// <param name="ReplyTemplate">Optional SMS reply merge template.</param>
/// <param name="TargetSystemCommunicationKey">Optional target system communication key.</param>
/// <param name="ContinueProcessing">Whether subsequent actions continue after matching.</param>
/// <param name="IsActive">Whether this rule is active.</param>
public sealed record SmsActionResponse(
    Guid Id,
    string Name,
    int Order,
    SmsKeywordMatchMode MatchMode,
    string? Keyword,
    SmsActionKind ActionKind,
    string? ReplyTemplate,
    string? TargetSystemCommunicationKey,
    bool ContinueProcessing,
    bool IsActive
);
