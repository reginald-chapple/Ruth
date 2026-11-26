using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class PipelineAutomationCollaborator(
    EngagementDbContext dbContext,
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher,
    IClock clock
)
{
    public async Task<Result<Guid>> ResolveOrCreatePublicRequesterAsync(
        SubmitPublicEngagementSignupCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.ExistingPersonId is { } existingId && existingId != Guid.Empty)
        {
            return existingId;
        }

        if (
            string.IsNullOrWhiteSpace(command.FirstName)
            || string.IsNullOrWhiteSpace(command.LastName)
            || string.IsNullOrWhiteSpace(command.Email)
        )
        {
            return EngagementErrors.EngagementRequestInvalid;
        }

        var normalizedEmail = command.Email.Trim();
        var search = await queryDispatcher.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(new SearchPeopleQuery(Query: normalizedEmail, MaxResults: 5), cancellationToken);
        if (search.IsSuccess)
        {
            var exact = search.Value.FirstOrDefault(p =>
                string.Equals(p.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)
            );
            if (exact is not null)
            {
                return exact.Id;
            }
        }

        IReadOnlyList<PhoneNumberInputDto>? phones = string.IsNullOrWhiteSpace(command.MobilePhone)
            ? null
            : [new PhoneNumberInputDto(PhoneNumberType.Mobile, command.MobilePhone.Trim(), true)];

        var create = await commandDispatcher.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                command.FirstName.Trim(),
                command.LastName.Trim(),
                Email: normalizedEmail,
                PrimaryBranchId: command.BranchId,
                PhoneNumbers: phones
            ),
            cancellationToken
        );

        return create.IsFailure
            ? Result.Failure<Guid>(create.Errors)
            : create.Value.Id;
    }

    public async Task ExecuteTransitionSideEffectsAsync(
        EngagementRequest request,
        StageTransitionOutcome outcome,
        CancellationToken cancellationToken
    )
    {
        var now = clock.UtcNow;
        if (outcome.PreviousStageId != outcome.NewStageId)
        {
            dbContext.EnqueueIntegrationEvent(
                new EngagementRequestStageChangedIntegrationEvent(
                    Guid.CreateVersion7(),
                    now,
                    request.Id.Value,
                    request.PipelineTypeId,
                    outcome.PreviousStageId,
                    outcome.NewStageId,
                    request.RequesterPersonId
                )
            );
        }

        foreach (var automation in outcome.TriggeredAutomations)
        {
            await ApplyAutomationAsync(request, automation, now, cancellationToken);
        }

        if (outcome.BecameConnected)
        {
            await CompletePlacementAndEnqueueConnectedAsync(request, now, cancellationToken);
        }
    }

    public async Task CompletePlacementAndEnqueueConnectedAsync(
        EngagementRequest request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (request.PlacementGroupId is { } groupId && groupId != Guid.Empty)
        {
            _ = await commandDispatcher.SendAsync<
                SimpleRegisterForGroupCommand,
                GroupMemberResponse
            >(
                new SimpleRegisterForGroupCommand(
                    groupId,
                    request.RequesterPersonId,
                    request.PlacementGroupRoleId,
                    GroupMemberStatus.Active,
                    Note: "Placed via Engagement Pipeline"
                ),
                cancellationToken
            );
        }

        dbContext.EnqueueIntegrationEvent(
            new EngagementRequestConnectedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                request.Id.Value,
                request.PipelineTypeId,
                request.EngagementOpportunityId,
                request.RequesterPersonId,
                request.AssignedConnectorPersonId,
                request.PlacementGroupId
            )
        );
    }

    private async Task ApplyAutomationAsync(
        EngagementRequest request,
        StageAutomation automation,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        switch (automation.AutomationKind)
        {
            case StageAutomationKind.AddToPlacementGroup
                when automation.TargetEntityId is { } groupId && groupId != Guid.Empty:
                _ = await commandDispatcher.SendAsync<
                    SimpleRegisterForGroupCommand,
                    GroupMemberResponse
                >(
                    new SimpleRegisterForGroupCommand(
                        groupId,
                        request.RequesterPersonId,
                        request.PlacementGroupRoleId
                    ),
                    cancellationToken
                );
                break;

            case StageAutomationKind.SendSystemNotification
                when !string.IsNullOrWhiteSpace(automation.SystemCommunicationKey):
                dbContext.EnqueueIntegrationEvent(
                    new SystemNotificationRequestedIntegrationEvent(
                        Guid.CreateVersion7(),
                        nowUtc,
                        automation.SystemCommunicationKey!,
                        RecipientPersonId: request.RequesterPersonId,
                        TriggerModule: "Engagement.Pipelines"
                    )
                );
                break;

            case StageAutomationKind.TransferToOpportunity
                when automation.TargetEntityId is { } targetOppId && targetOppId != Guid.Empty:
                await TransferInternalAsync(request, targetOppId, nowUtc, cancellationToken);
                break;
        }
    }

    private async Task TransferInternalAsync(
        EngagementRequest request,
        Guid targetOppId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var opp = await dbContext.EngagementOpportunities.FirstOrDefaultAsync(
            o => o.Id == EngagementOpportunityId.From(targetOppId),
            cancellationToken
        );
        if (opp is null)
        {
            return;
        }

        var pipe = await dbContext
            .PipelineTypes.Include(static p => p.Stages)
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(opp.PipelineTypeId),
                cancellationToken
            );
        if (pipe is not null)
        {
            request.TransferTo(pipe, opp, "Automated Stage Transfer", null, nowUtc);
        }
    }
}
