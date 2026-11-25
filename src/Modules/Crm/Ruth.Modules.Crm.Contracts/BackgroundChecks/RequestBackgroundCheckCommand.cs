using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Command to initiate a background check for a person via a registered provider.</summary>
public sealed record RequestBackgroundCheckCommand(
    Guid PersonId,
    string ProviderKey,
    string PackageName,
    Guid? RequestedByUserId = null
) : ICommand<BackgroundCheckResponse>;
