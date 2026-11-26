using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Actions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class GetMyWorkflowTasksQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetMyWorkflowTasksQuery, IReadOnlyList<WorkflowTaskItemResponse>>
{
    public async Task<Result<IReadOnlyList<WorkflowTaskItemResponse>>> HandleAsync(
        GetMyWorkflowTasksQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var workflows = await dbContext
            .Workflows.AsNoTracking()
            .AsSplitQuery()
            .Include(static w => w.Activities)
                .ThenInclude(static a => a.Actions)
            .Where(static w =>
                w.Status == WorkflowInstanceStatus.WaitingForInput
                || w.Status == WorkflowInstanceStatus.Active
            )
            .OrderByDescending(static w => w.ActivatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var typeNames = await dbContext
            .WorkflowTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        var tasks = new List<WorkflowTaskItemResponse>();
        foreach (var wf in workflows)
        {
            var typeName = typeNames.GetValueOrDefault(wf.WorkflowTypeId, "Workflow");
            AppendOpenTasks(wf, typeName, query, tasks);
        }

        return Result.Success<IReadOnlyList<WorkflowTaskItemResponse>>(tasks);
    }

    private static void AppendOpenTasks(
        WorkflowInstance wf,
        string typeName,
        GetMyWorkflowTasksQuery query,
        List<WorkflowTaskItemResponse> tasks
    )
    {
        foreach (
            var act in wf.Activities.Where(static a => !a.IsCompleted).OrderBy(static a => a.Order)
        )
        {
            if (!MatchesAssigneeFilter(act.AssignedPersonId, act.AssignedGroupId, query))
            {
                continue;
            }

            var formAction = act
                .Actions.OrderBy(static x => x.Order)
                .FirstOrDefault(static x =>
                    !x.IsCompleted
                    && string.Equals(
                        x.ActionPluginKey,
                        UserEntryFormWorkflowAction.Key,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            if (formAction is not null)
            {
                tasks.Add(
                    new WorkflowTaskItemResponse(
                        wf.Id.Value,
                        wf.WorkflowTypeId.Value,
                        typeName,
                        wf.WorkflowNumber,
                        wf.Name,
                        wf.StateLabel,
                        act.Id.Value,
                        act.Name,
                        formAction.Id.Value,
                        formAction.Name,
                        act.AssignedPersonId,
                        act.AssignedGroupId,
                        act.ActivatedAtUtc
                    )
                );
            }
        }
    }

    private static bool MatchesAssigneeFilter(
        Guid? personId,
        Guid? groupId,
        GetMyWorkflowTasksQuery query
    )
    {
        return !query.AssignedPersonId.HasValue && !query.AssignedGroupId.HasValue
            ? query.IncludeUnassigned || personId.HasValue || groupId.HasValue
            : MatchesTargetPersonOrGroup(personId, groupId, query)
                || MatchesFallbackUnassigned(personId, groupId, query.IncludeUnassigned);
    }

    private static bool MatchesTargetPersonOrGroup(
        Guid? personId,
        Guid? groupId,
        GetMyWorkflowTasksQuery query
    )
    {
        var personMatch = query.AssignedPersonId.HasValue && personId == query.AssignedPersonId;
        var groupMatch = query.AssignedGroupId.HasValue && groupId == query.AssignedGroupId;
        return personMatch || groupMatch;
    }

    private static bool MatchesFallbackUnassigned(
        Guid? personId,
        Guid? groupId,
        bool includeUnassigned
    )
    {
        return includeUnassigned && !personId.HasValue && !groupId.HasValue;
    }
}
