using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Creates a new reusable FormBuilderTemplate.</summary>
/// <param name="Spec">Template specification.</param>
public sealed record CreateFormBuilderTemplateCommand(FormBuilderTemplateSpec Spec)
    : ICommand<FormBuilderTemplateResponse>;
