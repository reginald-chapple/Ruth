using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class SubmitPublicEngagementSignupCommandHandler(
    EngagementDbContext dbContext,
    PipelineAutomationCollaborator collaborator,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<SubmitPublicEngagementSignupCommand, EngagementRequestResponse>
{
    public async Task<Result<EngagementRequestResponse>> HandleAsync(
        SubmitPublicEngagementSignupCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OpportunityIdOrSlug))
        {
            return EngagementErrors.OpportunityNotFound;
        }

        var token = command.OpportunityIdOrSlug.Trim();
        var oppQuery = dbContext
            .EngagementOpportunities.AsNoTracking()
            .Where(static o => o.IsActive && o.IsPublic);
        var opp = Guid.TryParse(token, out var oppId)
            ? await oppQuery.FirstOrDefaultAsync(
                o => o.Id == EngagementOpportunityId.From(oppId),
                cancellationToken
            )
            : await oppQuery.FirstOrDefaultAsync(
                o => o.Slug == token.ToLowerInvariant(),
                cancellationToken
            );
        if (opp is null)
        {
            return EngagementErrors.OpportunityNotFound;
        }

        var pipeline = await dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(opp.PipelineTypeId) && p.IsActive,
                cancellationToken
            );
        if (pipeline is null)
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var personRes = await collaborator.ResolveOrCreatePublicRequesterAsync(
            command,
            cancellationToken
        );
        if (personRes.IsFailure)
        {
            return Result.Failure<EngagementRequestResponse>(personRes.Errors);
        }

        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var created = EngagementRequest.Create(
            pipeline,
            opp,
            new EngagementRequestCreationSpec(
                personRes.Value,
                command.BranchId,
                Comments: command.Comments
            ),
            today,
            now
        );
        if (created.IsFailure)
        {
            return Result.Failure<EngagementRequestResponse>(created.Errors);
        }

        _ = dbContext.EngagementRequests.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [personRes.Value],
            cancellationToken
        );
        return PipelineProjectionMapper.ToResponse(
            created.Value,
            pipeline,
            opp.Name,
            people,
            today,
            now
        );
    }
}
