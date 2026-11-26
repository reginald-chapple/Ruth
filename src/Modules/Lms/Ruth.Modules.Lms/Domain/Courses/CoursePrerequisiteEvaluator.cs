using Ruth.Modules.Lms.Contracts.Courses;

namespace Ruth.Modules.Lms.Domain.Courses;

/// <summary>Evaluates course prerequisite completion and detects prerequisite graph cycles.</summary>
public static class CoursePrerequisiteEvaluator
{
    public static bool ArePrerequisitesMet(
        LearningCourse course,
        IReadOnlySet<LearningCourseId> completedCourseIds
    )
    {
        ArgumentNullException.ThrowIfNull(course);
        ArgumentNullException.ThrowIfNull(completedCourseIds);

        var prereqs = course
            .Requirements.Where(static r => r.RequirementType == CourseRequirementType.Prerequisite)
            .ToList();
        var equivalents = course
            .Requirements.Where(static r => r.RequirementType == CourseRequirementType.Equivalent)
            .Select(static r => r.RequiredLearningCourseId)
            .ToHashSet();

        return (equivalents.Count > 0 && equivalents.Any(completedCourseIds.Contains))
            || prereqs.TrueForAll(p => completedCourseIds.Contains(p.RequiredLearningCourseId));
    }

    public static bool WouldCreateCycle(
        LearningCourseId targetCourseId,
        LearningCourseId newRequiredCourseId,
        IReadOnlyDictionary<LearningCourseId, IReadOnlyList<LearningCourseId>> prerequisiteGraph
    )
    {
        if (targetCourseId == newRequiredCourseId)
        {
            return true;
        }

        var visited = new HashSet<LearningCourseId>();
        var stack = new Stack<LearningCourseId>();
        stack.Push(newRequiredCourseId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == targetCourseId)
            {
                return true;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            if (prerequisiteGraph.TryGetValue(current, out var deps))
            {
                foreach (var dep in deps)
                {
                    stack.Push(dep);
                }
            }
        }

        return false;
    }
}
