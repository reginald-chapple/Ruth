namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Summary returned after executing a drip flow advancement pass.</summary>
/// <param name="RecipientsEvaluated">Due recipients evaluated in this pass.</param>
/// <param name="StepsDispatched">Drip messages sent via configured transports.</param>
/// <param name="StepsSkippedSmartRule">Steps skipped due to SendOnlyIfPreviousUnopened.</param>
/// <param name="RecipientsExitedUnsubscribed">Recipients exited due to opt-out or suppression.</param>
/// <param name="RecipientsCompletedFlow">Recipients reaching the end of the flow.</param>
public sealed record FlowAdvancementBatchResultResponse(
    int RecipientsEvaluated,
    int StepsDispatched,
    int StepsSkippedSmartRule,
    int RecipientsExitedUnsubscribed,
    int RecipientsCompletedFlow
);
