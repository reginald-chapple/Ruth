using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class AddGroupMemberCommandHandler(GroupMemberAddService addService)
    : ICommandHandler<AddGroupMemberCommand, GroupMemberResponse>
{
    public Task<Result<GroupMemberResponse>> HandleAsync(
        AddGroupMemberCommand command,
        CancellationToken cancellationToken = default
    )
    {
        return addService.AddMemberAsync(command, cancellationToken);
    }
}
