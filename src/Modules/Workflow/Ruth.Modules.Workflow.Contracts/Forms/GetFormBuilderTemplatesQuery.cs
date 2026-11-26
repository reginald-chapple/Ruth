using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Lists reusable FormBuilderTemplates.</summary>
/// <param name="IncludeInactive">When true, includes inactive templates.</param>
public sealed record GetFormBuilderTemplatesQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<FormBuilderTemplateResponse>>;
