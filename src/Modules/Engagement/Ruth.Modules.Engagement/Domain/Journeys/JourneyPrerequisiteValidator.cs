using Ruth.Modules.Engagement.Contracts.Journeys;

namespace Ruth.Modules.Engagement.Domain.Journeys;

/// <summary>Clean-room DAG validator and completion evaluator for JourneyProgram milestone prerequisites.</summary>
public static class JourneyPrerequisiteValidator
{
    /// <summary>Verifies that milestone codes are unique, prerequisite codes resolve, and no cycle exists.</summary>
    public static bool ValidateAcyclicPrerequisites(IReadOnlyList<MilestoneTypeSpec> specs)
    {
        if (specs is null || specs.Count == 0)
        {
            return false;
        }

        var codeMap = new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var spec in specs)
        {
            if (string.IsNullOrWhiteSpace(spec.Name) || string.IsNullOrWhiteSpace(spec.Code))
            {
                return false;
            }

            var code = spec.Code.Trim().ToUpperInvariant();
            if (!codeMap.TryAdd(code, spec.PrerequisiteCodes ?? []))
            {
                return false;
            }
        }

        return HasNoUnknownEdgesOrCycles(codeMap);
    }

    /// <summary>Evaluates whether a person's completed milestone set satisfies all prerequisites for a target milestone.</summary>
    public static bool ArePrerequisitesMet(
        MilestoneType target,
        IReadOnlySet<Guid> completedMilestoneTypeIds
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(completedMilestoneTypeIds);
        return target.PrerequisiteMilestoneTypeIds.All(completedMilestoneTypeIds.Contains);
    }

    /// <summary>Evaluates whether a JourneyProgram's completion mode has been satisfied.</summary>
    public static bool IsProgramComplete(
        JourneyProgram program,
        IReadOnlySet<Guid> completedMilestoneTypeIds
    )
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(completedMilestoneTypeIds);

        var required = program
            .MilestoneTypes.Where(static m => m.IsActive && m.IsRequiredForProgramCompletion)
            .ToArray();
        return required.Length > 0
            && (
                program.CompletionMode == JourneyCompletionMode.AnyOneRequiredMilestone
                    ? required.Any(m => completedMilestoneTypeIds.Contains(m.Id.Value))
                    : required.All(m => completedMilestoneTypeIds.Contains(m.Id.Value))
            );
    }

    private static bool HasNoUnknownEdgesOrCycles(
        Dictionary<string, IReadOnlyList<string>> adjacency
    )
    {
        var visitState = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in adjacency.Keys)
        {
            if (!VisitNode(code, adjacency, visitState))
            {
                return false;
            }
        }

        return true;
    }

    private static bool VisitNode(
        string node,
        Dictionary<string, IReadOnlyList<string>> adjacency,
        Dictionary<string, int> state
    )
    {
        if (state.TryGetValue(node, out var existing))
        {
            return existing == 2;
        }

        state[node] = 1;
        foreach (var rawPrereq in adjacency[node])
        {
            if (string.IsNullOrWhiteSpace(rawPrereq))
            {
                continue;
            }

            var prereq = rawPrereq.Trim().ToUpperInvariant();
            if (!adjacency.ContainsKey(prereq) || !VisitNode(prereq, adjacency, state))
            {
                return false;
            }
        }

        state[node] = 2;
        return true;
    }
}
