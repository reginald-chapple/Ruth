using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Domain.Journeys;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal static class JourneyProjectionMapper
{
    public static JourneyProgramResponse ToResponse(JourneyProgram program)
    {
        var codeById = program.MilestoneTypes.ToDictionary(
            static m => m.Id.Value,
            static m => m.Code
        );

        return new JourneyProgramResponse(
            program.Id.Value,
            program.Name,
            program.Slug,
            program.Description,
            program.CategoryId,
            program.DefaultListView,
            program.CompletionMode,
            program.IsActive,
            [
                .. program
                    .Statuses.OrderBy(static s => s.Order)
                    .Select(static s => new MilestoneStatusResponse(
                        s.Id.Value,
                        s.Name,
                        s.ColorHex,
                        s.IsCompleteStatus,
                        s.Order
                    )),
            ],
            [
                .. program
                    .MilestoneTypes.OrderBy(static m => m.Order)
                    .Select(m => new MilestoneTypeResponse(
                        m.Id.Value,
                        m.Name,
                        m.Code,
                        m.Description,
                        m.IconCssClass,
                        m.Order,
                        m.IsRequiredForProgramCompletion,
                        m.AllowMultipleCompletions,
                        m.HasEndDate,
                        [.. m.PrerequisiteMilestoneTypeIds],
                        [
                            .. m
                                .PrerequisiteMilestoneTypeIds.Where(codeById.ContainsKey)
                                .Select(id => codeById[id]),
                        ],
                        m.IsActive
                    )),
            ],
            new Dictionary<string, string?>(
                program.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            program.CreatedOnUtc
        );
    }

    public static PersonMilestoneResponse ToResponse(PersonMilestone item, JourneyProgram program)
    {
        var mtName = program.FindMilestoneType(item.MilestoneTypeId)?.Name ?? "Milestone";
        var stName = program.FindStatus(item.MilestoneStatusId)?.Name ?? "Status";
        return new PersonMilestoneResponse(
            item.Id.Value,
            item.PersonId,
            item.JourneyProgramId,
            item.MilestoneTypeId,
            mtName,
            item.MilestoneStatusId,
            stName,
            item.IsCompleted,
            item.BranchId,
            item.StartDate,
            item.CompletedDate,
            item.EndDate,
            item.Note
        );
    }
}
