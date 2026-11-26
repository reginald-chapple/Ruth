using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Journeys;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Achievements;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal sealed class RecordPersonMilestoneCommandHandler(
    EngagementDbContext dbContext,
    AchievementProgressionCollaborator achievements,
    IClock clock
) : ICommandHandler<RecordPersonMilestoneCommand, PersonMilestoneResponse>
{
    public async Task<Result<PersonMilestoneResponse>> HandleAsync(
        RecordPersonMilestoneCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var program = await dbContext
            .JourneyPrograms.AsNoTracking()
            .Include(static p => p.Statuses)
            .Include(static p => p.MilestoneTypes)
            .FirstOrDefaultAsync(
                p => p.Id == JourneyProgramId.From(command.JourneyProgramId),
                cancellationToken
            );
        if (program is null)
        {
            return EngagementErrors.JourneyProgramNotFound;
        }

        var milestoneType = program.FindMilestoneType(command.MilestoneTypeId);
        var status = program.FindStatus(command.MilestoneStatusId);
        if (milestoneType is null || status is null)
        {
            return EngagementErrors.PersonMilestoneInvalid;
        }

        var existing = await dbContext
            .PersonMilestones.AsNoTracking()
            .Where(m =>
                m.PersonId == command.PersonId && m.JourneyProgramId == command.JourneyProgramId
            )
            .ToListAsync(cancellationToken);
        var completedTypeIds = existing
            .Where(static m => m.IsCompleted)
            .Select(static m => m.MilestoneTypeId)
            .ToHashSet();

        var created = PersonMilestone.Record(
            command.PersonId,
            program,
            milestoneType,
            status,
            command.StartDate,
            command.CompletedDate,
            command.EndDate,
            command.BranchId,
            command.Note,
            completedTypeIds
        );
        if (created.IsFailure)
        {
            return Result.Failure<PersonMilestoneResponse>(created.Errors);
        }

        _ = dbContext.PersonMilestones.Add(created.Value);
        if (created.Value.IsCompleted)
        {
            await HandleCompletionRollupsAsync(
                created.Value,
                program,
                existing,
                completedTypeIds,
                cancellationToken
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return JourneyProjectionMapper.ToResponse(created.Value, program);
    }

    private async Task HandleCompletionRollupsAsync(
        PersonMilestone milestone,
        JourneyProgram program,
        List<PersonMilestone> existingProgramMilestones,
        HashSet<Guid> completedTypeIds,
        CancellationToken cancellationToken
    )
    {
        var now = clock.UtcNow;
        var completedDate = milestone.CompletedDate ?? milestone.StartDate;
        _ = completedTypeIds.Add(milestone.MilestoneTypeId);

        dbContext.EnqueueIntegrationEvent(
            new PersonMilestoneCompletedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                milestone.Id.Value,
                milestone.PersonId,
                milestone.JourneyProgramId,
                milestone.MilestoneTypeId,
                completedDate
            )
        );

        var alreadyCompletedProgram = await dbContext.JourneyCompletions.AnyAsync(
            c => c.PersonId == milestone.PersonId && c.JourneyProgramId == program.Id.Value,
            cancellationToken
        );
        var newCompletionsAdded = 0;
        if (
            !alreadyCompletedProgram
            && JourneyPrerequisiteValidator.IsProgramComplete(program, completedTypeIds)
        )
        {
            var earliestStart = existingProgramMilestones
                .Select(static m => m.StartDate)
                .Append(milestone.StartDate)
                .Min();
            var completion = JourneyCompletion.Create(
                milestone.PersonId,
                program.Id.Value,
                milestone.BranchId,
                earliestStart,
                completedDate
            );
            _ = dbContext.JourneyCompletions.Add(completion);
            newCompletionsAdded = 1;
            dbContext.EnqueueIntegrationEvent(
                new JourneyProgramCompletedIntegrationEvent(
                    Guid.CreateVersion7(),
                    now,
                    completion.Id.Value,
                    milestone.PersonId,
                    program.Id.Value,
                    completedDate
                )
            );
        }

        var totalJourneyCompletions =
            await dbContext.JourneyCompletions.CountAsync(
                c => c.PersonId == milestone.PersonId,
                cancellationToken
            ) + newCompletionsAdded;
        var totalCompletedMilestones = existingProgramMilestones.Count(static m => m.IsCompleted) + 1;

        await achievements.EvaluateMilestoneAndJourneyAchievementsAsync(
            milestone.PersonId,
            program.Id.Value,
            milestone.MilestoneTypeId,
            totalCompletedMilestones,
            totalJourneyCompletions,
            completedDate,
            cancellationToken
        );
    }
}
