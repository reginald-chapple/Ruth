using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Actions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class LaunchFormBuilderSubmissionCommandHandler(
    WorkflowDbContext dbContext,
    ICommandDispatcher commandDispatcher
) : ICommandHandler<LaunchFormBuilderSubmissionCommand, WorkflowFormSubmissionResponse>
{
    public async Task<Result<WorkflowFormSubmissionResponse>> HandleAsync(
        LaunchFormBuilderSubmissionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var template = await ResolveTemplateAsync(command.TemplateIdOrSlug, cancellationToken);
        if (template is null || !template.IsActive || !template.LinkedWorkflowTypeId.HasValue)
        {
            return Result.Failure<WorkflowFormSubmissionResponse>(
                WorkflowErrors.FormTemplateNotFound
            );
        }

        var launchRes = await commandDispatcher.SendAsync<
            LaunchWorkflowCommand,
            WorkflowInstanceResponse
        >(
            new LaunchWorkflowCommand(
                template.LinkedWorkflowTypeId.Value.ToString("D", CultureInfo.InvariantCulture),
                Name: template.Name,
                InitiatorPersonId: command.InitiatorPersonId,
                InitialAttributes: command.FieldValues,
                ProcessImmediately: true
            ),
            cancellationToken
        );
        return launchRes.IsFailure
            ? Result.Failure<WorkflowFormSubmissionResponse>(launchRes.PrimaryError)
            : await AutoSubmitOrReturnAsync(command, template, launchRes.Value, cancellationToken);
    }

    private async Task<FormBuilderTemplate?> ResolveTemplateAsync(
        string? idOrSlug,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            return null;
        }

        var slugCandidate = idOrSlug.Trim().ToLowerInvariant();
        return Guid.TryParse(idOrSlug, out var parsed)
            ? await dbContext
                .FormBuilderTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == FormBuilderTemplateId.From(parsed), ct)
            : await dbContext
                .FormBuilderTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slugCandidate, ct);
    }

    private async Task<Result<WorkflowFormSubmissionResponse>> AutoSubmitOrReturnAsync(
        LaunchFormBuilderSubmissionCommand command,
        FormBuilderTemplate template,
        WorkflowInstanceResponse launched,
        CancellationToken ct
    )
    {
        var waitingAction = launched
            .Activities.Where(static a => !a.IsCompleted)
            .SelectMany(static a => a.Actions)
            .FirstOrDefault(static act =>
                !act.IsCompleted
                && string.Equals(
                    act.ActionPluginKey,
                    UserEntryFormWorkflowAction.Key,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        return waitingAction is not null && !string.IsNullOrWhiteSpace(command.SelectedButtonKey)
            ? await commandDispatcher.SendAsync<
                SubmitWorkflowEntryFormCommand,
                WorkflowFormSubmissionResponse
            >(
                new SubmitWorkflowEntryFormCommand(
                    launched.Id,
                    waitingAction.Id,
                    command.SelectedButtonKey,
                    command.FieldValues
                        ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
                    ActorPersonId: command.InitiatorPersonId,
                    SubmitterFirstName: command.SubmitterFirstName,
                    SubmitterLastName: command.SubmitterLastName,
                    SubmitterEmail: command.SubmitterEmail
                ),
                ct
            )
            : Result.Success(
                new WorkflowFormSubmissionResponse(
                    launched,
                    command.SelectedButtonKey ?? "launch",
                    template.ConfirmationTitle,
                    $"Started {launched.WorkflowNumber}.",
                    template.CompletionRedirectUrl
                )
            );
    }
}
