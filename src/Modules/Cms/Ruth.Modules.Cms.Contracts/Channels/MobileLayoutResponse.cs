namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Represents a layout shell definition inside a MobileApplication.</summary>
public sealed record MobileLayoutResponse(
    Guid Id,
    Guid MobileApplicationId,
    string Name,
    string LayoutKey,
    string ShellMarkup,
    int Order
);
