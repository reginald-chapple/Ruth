using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class PreviewFormVisibilityQueryHandler
    : IQueryHandler<PreviewFormVisibilityQuery, FormVisibilityPreviewResponse>
{
    public Task<Result<FormVisibilityPreviewResponse>> HandleAsync(
        PreviewFormVisibilityQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (query.FormSpec is null || query.FormSpec.Fields is null)
        {
            return Task.FromResult(
                Result.Failure<FormVisibilityPreviewResponse>(WorkflowErrors.ActionFormInvalid)
            );
        }

        var preview = FormVisibilityEvaluator.EvaluatePreview(query.FormSpec, query.SampleValues);
        return Task.FromResult(Result.Success(preview));
    }
}
