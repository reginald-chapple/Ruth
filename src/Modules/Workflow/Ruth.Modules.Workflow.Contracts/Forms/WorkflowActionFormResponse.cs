namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Read projection of a WorkflowActionForm definition.</summary>
/// <param name="Id">Form identifier.</param>
/// <param name="WorkflowActionTypeId">Owning WorkflowActionType identifier.</param>
/// <param name="FormBuilderTemplateId">Optional reusable FormBuilderTemplate identifier.</param>
/// <param name="HeaderMarkup">Optional header markup.</param>
/// <param name="FooterMarkup">Optional footer markup.</param>
/// <param name="AllowPersonEntry">Whether person contact capture is enabled.</param>
/// <param name="NotificationSystemCommunicationKey">Optional notification template key.</param>
/// <param name="Sections">Ordered sections.</param>
/// <param name="Fields">Ordered fields.</param>
/// <param name="Buttons">Ordered outcome buttons.</param>
public sealed record WorkflowActionFormResponse(
    Guid Id,
    Guid WorkflowActionTypeId,
    Guid? FormBuilderTemplateId,
    string? HeaderMarkup,
    string? FooterMarkup,
    bool AllowPersonEntry,
    string? NotificationSystemCommunicationKey,
    IReadOnlyList<WorkflowActionFormSectionResponse> Sections,
    IReadOnlyList<WorkflowActionFormFieldResponse> Fields,
    IReadOnlyList<WorkflowFormButtonSpec> Buttons
);
