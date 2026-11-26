using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.TwoWaySms;

/// <summary>Single keyword-matching action rule owned by an <see cref="SmsPipeline" />.</summary>
public sealed class SmsAction : Entity<SmsActionId>
{
    private SmsAction() { }

    private SmsAction(
        SmsActionId id,
        SmsPipelineId pipelineId,
        string name,
        int order,
        SmsKeywordMatchMode matchMode,
        string? keyword,
        SmsActionKind actionKind,
        string? replyTemplate,
        string? targetSystemCommunicationKey,
        bool continueProcessing,
        bool isActive
    )
        : base(id)
    {
        SmsPipelineId = pipelineId;
        Name = name;
        Order = order;
        MatchMode = matchMode;
        Keyword = keyword;
        ActionKind = actionKind;
        ReplyTemplate = replyTemplate;
        TargetSystemCommunicationKey = targetSystemCommunicationKey;
        ContinueProcessing = continueProcessing;
        IsActive = isActive;
    }

    public SmsPipelineId SmsPipelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public SmsKeywordMatchMode MatchMode { get; private set; }
    public string? Keyword { get; private set; }
    public SmsActionKind ActionKind { get; private set; }
    public string? ReplyTemplate { get; private set; }
    public string? TargetSystemCommunicationKey { get; private set; }
    public bool ContinueProcessing { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Creates a validated <see cref="SmsAction" /> from a contract DTO.</summary>
    public static Result<SmsAction> Create(
        SmsPipelineId pipelineId,
        SmsActionInputDto dto,
        int normalizedOrder
    )
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 120)
        {
            return Result.Failure<SmsAction>(BulkCommunicationErrors.SmsPipelineActionsInvalid);
        }

        var keyword = string.IsNullOrWhiteSpace(dto.Keyword)
            ? null
            : dto.Keyword.Trim().ToUpperInvariant();
        var isValid =
            (
                dto.MatchMode == SmsKeywordMatchMode.FallbackAny
                || !string.IsNullOrWhiteSpace(keyword)
            ) && ValidateActionTarget(dto);

        return !isValid
            ? Result.Failure<SmsAction>(BulkCommunicationErrors.SmsPipelineActionsInvalid)
            : new SmsAction(
                SmsActionId.Create(),
                pipelineId,
                dto.Name.Trim(),
                normalizedOrder,
                dto.MatchMode,
                keyword,
                dto.ActionKind,
                Clean(dto.ReplyTemplate),
                Clean(dto.TargetSystemCommunicationKey),
                dto.ContinueProcessing,
                dto.IsActive
            );
    }

    /// <summary>Determines whether <paramref name="inboundBody" /> satisfies this action's match rule.</summary>
    public bool IsMatch(string inboundBody)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(inboundBody))
        {
            return false;
        }

        var trimmed = inboundBody.Trim();
        return MatchMode == SmsKeywordMatchMode.FallbackAny
            || (
                !string.IsNullOrWhiteSpace(Keyword)
                && MatchMode switch
                {
                    SmsKeywordMatchMode.ExactKeyword => string.Equals(
                        trimmed,
                        Keyword,
                        StringComparison.OrdinalIgnoreCase
                    ),
                    SmsKeywordMatchMode.StartsWithKeyword => MatchesFirstWord(trimmed, Keyword),
                    SmsKeywordMatchMode.ContainsKeyword => trimmed.Contains(
                        Keyword,
                        StringComparison.OrdinalIgnoreCase
                    ),
                    SmsKeywordMatchMode.FallbackAny or _ => false,
                }
            );
    }

    private static bool MatchesFirstWord(string trimmed, string keyword)
    {
        var spaceIdx = trimmed.IndexOfAny([' ', '\t', '\r', '\n']);
        var firstWord = spaceIdx < 0 ? trimmed : trimmed[..spaceIdx];
        return string.Equals(firstWord, keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ValidateActionTarget(SmsActionInputDto dto)
    {
        return dto.ActionKind switch
        {
            SmsActionKind.AutoReply => !string.IsNullOrWhiteSpace(dto.ReplyTemplate)
                && dto.ReplyTemplate.Trim().Length <= 1_600,
            SmsActionKind.PublishSystemNotification => !string.IsNullOrWhiteSpace(
                dto.TargetSystemCommunicationKey
            ),
            SmsActionKind.OptOutSms or SmsActionKind.OptInSms or _ => dto.ReplyTemplate is null
                || dto.ReplyTemplate.Trim().Length <= 1_600,
        };
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
