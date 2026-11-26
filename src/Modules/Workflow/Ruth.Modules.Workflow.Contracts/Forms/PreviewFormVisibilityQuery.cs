using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Evaluates conditional visibility rules on an ad-hoc form specification in the Visual Form Builder studio.</summary>
/// <param name="FormSpec">Form specification to preview.</param>
/// <param name="SampleValues">Sample attribute/field values to test against.</param>
public sealed record PreviewFormVisibilityQuery(
    WorkflowActionFormSpec FormSpec,
    IReadOnlyDictionary<string, string?>? SampleValues = null
) : IQuery<FormVisibilityPreviewResponse>;
