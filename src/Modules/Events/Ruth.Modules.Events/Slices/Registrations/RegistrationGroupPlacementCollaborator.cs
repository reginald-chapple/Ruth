using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class RegistrationGroupPlacementCollaborator(ICommandDispatcher commandDispatcher)
{
    public async Task AutoPlaceConfirmedRegistrantsAsync(
        Registration registration,
        RegistrationInstance instance,
        RegistrationTemplate template,
        CancellationToken cancellationToken
    )
    {
        var autoPlacement = template.GroupPlacements.FirstOrDefault(static p =>
            p.AutoPlaceOnConfirmation
        );
        var targetGroupId = instance.LinkedTargetGroupId ?? autoPlacement?.DefaultTargetGroupId;
        if (autoPlacement is null || targetGroupId is not { } groupId || groupId == Guid.Empty)
        {
            return;
        }

        foreach (
            var registrant in registration.Registrants.Where(static r =>
                r.Status == RegistrantStatus.Confirmed && r.PersonId.HasValue
            )
        )
        {
            var placed = await commandDispatcher.SendAsync<
                AddGroupMemberCommand,
                GroupMemberResponse
            >(
                new AddGroupMemberCommand(
                    groupId,
                    registrant.PersonId!.Value,
                    autoPlacement.DefaultGroupRoleId,
                    GroupMemberStatus.Active,
                    Note: $"Auto-placed from registration {registration.ConfirmationCode}",
                    SkipRequirementsCheck: true
                ),
                cancellationToken
            );
            if (placed.IsSuccess)
            {
                registrant.MarkPlacedInGroup(groupId, placed.Value.Id);
            }
        }
    }

    public async Task<Result<Guid>> PlaceSingleRegistrantAsync(
        RegistrationRegistrant registrant,
        Guid targetGroupId,
        Guid? targetGroupRoleId,
        string confirmationCode,
        CancellationToken cancellationToken
    )
    {
        if (registrant.PersonId is not { } personId || personId == Guid.Empty)
        {
            return EventsRegistrationErrors.FormValidationFailed;
        }

        var placed = await commandDispatcher.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(
                targetGroupId,
                personId,
                targetGroupRoleId,
                GroupMemberStatus.Active,
                Note: $"Placed from registration {confirmationCode}",
                SkipRequirementsCheck: true
            ),
            cancellationToken
        );
        if (placed.IsFailure)
        {
            return Result.Failure<Guid>(placed.Errors);
        }

        registrant.MarkPlacedInGroup(targetGroupId, placed.Value.Id);
        return placed.Value.Id;
    }
}
