using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Command to add a layout shell to a MobileApplication.</summary>
public sealed record AddMobileLayoutCommand(
    Guid MobileApplicationId,
    string Name,
    string LayoutKey,
    string ShellMarkup,
    int Order = 0
) : ICommand<MobileApplicationResponse>;
