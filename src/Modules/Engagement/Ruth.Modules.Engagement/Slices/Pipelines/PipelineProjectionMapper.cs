using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal static class PipelineProjectionMapper
{
    public static PipelineTypeResponse ToResponse(PipelineType entity)
    {
        return new PipelineTypeResponse(
            entity.Id.Value,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.EnableFutureFollowUp,
            entity.DefaultFollowUpDays,
            entity.DaysUntilRequestIdle,
            entity.RequiresPlacementGroup,
            entity.IsSequentialStageProgression,
            entity.IsActive,
            [
                .. entity
                    .Stages.OrderBy(static s => s.Order)
                    .Select(static s => new PipelineStageResponse(
                        s.Id.Value,
                        s.Name,
                        s.Description,
                        s.Order,
                        s.IsInitial,
                        s.IsTerminalCompleted,
                        s.IsTerminalInactive,
                        s.IdleThresholdDaysOverride,
                        [
                            .. s.Automations.Select(static a => new StageAutomationDto(
                                a.AutomationKind,
                                a.TargetEntityId,
                                a.SystemCommunicationKey,
                                a.AutoSetFollowUpDays
                            )),
                        ]
                    )),
            ],
            [
                .. entity
                    .ActivityTypes.OrderBy(static a => a.Order)
                    .Select(static a => new PipelineActivityTypeResponse(
                        a.Id.Value,
                        a.Name,
                        a.Order,
                        a.IsActive
                    )),
            ],
            new Dictionary<string, string?>(entity.AttributeValues, StringComparer.OrdinalIgnoreCase),
            entity.CreatedOnUtc
        );
    }

    public static EngagementOpportunityResponse ToResponse(
        EngagementOpportunity entity,
        string pipelineName
    )
    {
        return new EngagementOpportunityResponse(
            entity.Id.Value,
            entity.PipelineTypeId,
            pipelineName,
            entity.Name,
            entity.Slug,
            entity.PublicSummary,
            entity.Description,
            entity.IconCssClass,
            entity.PhotoBinaryFileId,
            entity.DefaultBranchId,
            entity.DefaultConnectorPersonId,
            [
                .. entity.BranchConfigs.Select(static b => new OpportunityBranchCoordinatorDto(
                    b.BranchId,
                    b.DefaultConnectorPersonId,
                    b.ConnectorGroupId
                )),
            ],
            [
                .. entity.PlacementGroupConfigs.Select(static p =>
                    new OpportunityPlacementGroupDto(
                        p.GroupId,
                        p.GroupRoleId,
                        p.BranchId,
                        p.UseAllGroupsOfType
                    )
                ),
            ],
            entity.IsActive,
            entity.IsPublic,
            new Dictionary<string, string?>(entity.AttributeValues, StringComparer.OrdinalIgnoreCase),
            entity.CreatedOnUtc
        );
    }

    public static EngagementRequestResponse ToResponse(
        EngagementRequest request,
        PipelineType pipeline,
        string opportunityName,
        IReadOnlyDictionary<Guid, (string Name, string? Email)> personDirectory,
        DateOnly today,
        DateTimeOffset nowUtc
    )
    {
        var (daysIdle, health) = PipelineBoardEvaluator.EvaluateHealth(
            request,
            pipeline,
            today,
            nowUtc
        );
        var stageName = pipeline.FindStage(request.CurrentStageId)?.Name ?? "Unknown Stage";
        var (personName, personEmail) = personDirectory.TryGetValue(
            request.RequesterPersonId,
            out var info
        )
            ? info
            : ($"Person {request.RequesterPersonId.ToString("D")[..8]}", null);

        return new EngagementRequestResponse(
            request.Id.Value,
            request.PipelineTypeId,
            pipeline.Name,
            request.EngagementOpportunityId,
            opportunityName,
            request.RequesterPersonId,
            personName,
            personEmail,
            request.AssignedConnectorPersonId,
            request.BranchId,
            request.CurrentStageId,
            stageName,
            request.PlacementGroupId,
            request.PlacementGroupRoleId,
            request.RequestState,
            health,
            daysIdle,
            request.FollowUpDate,
            request.Comments,
            request.LastActivityAtUtc,
            request.ConnectedAtUtc,
            request.CreatedOnUtc,
            [
                .. request
                    .Activities.OrderByDescending(static a => a.OccurredAtUtc)
                    .Select(static a => new EngagementRequestActivityResponse(
                        a.Id.Value,
                        a.ActivityTypeId,
                        a.ActivityName,
                        a.ConnectorPersonId,
                        a.Note,
                        a.OccurredAtUtc
                    )),
            ]
        );
    }

    public static async Task<Dictionary<Guid, (string Name, string? Email)>> LoadPeopleAsync(
        IQueryDispatcher queryDispatcher,
        IEnumerable<Guid> personIds,
        CancellationToken cancellationToken
    )
    {
        var map = new Dictionary<Guid, (string Name, string? Email)>();
        foreach (var id in personIds.Where(static g => g != Guid.Empty).Distinct())
        {
            var res = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(id, FollowRedirects: true),
                cancellationToken
            );
            if (res.IsSuccess)
            {
                map[id] = (res.Value.FullName, res.Value.Email);
            }
        }

        return map;
    }
}
