using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Updates an existing FormBuilderTemplate.</summary>
/// <param name="TemplateId">Target FormBuilderTemplate identifier.</param>
/// <param name="Spec">Updated template specification.</param>
public sealed record UpdateFormBuilderTemplateCommand(Guid TemplateId, FormBuilderTemplateSpec Spec)
    : ICommand<FormBuilderTemplateResponse>;
