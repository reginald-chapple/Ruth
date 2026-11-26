using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Command to create a native mobile application backend configuration.</summary>
public sealed record CreateMobileApplicationCommand(
    string Name,
    string Slug,
    string BundleIdentifier,
    Guid? SiteId = null,
    string ApiVersion = "1.0",
    string MinimumClientVersion = "1.0.0",
    string LatestClientVersion = "1.0.0",
    MobileNavigationType NavigationType = MobileNavigationType.TabBar,
    string PrimaryColorHex = "#0F6CBD",
    string SecondaryColorHex = "#115EA3",
    IReadOnlyDictionary<string, string?>? AdditionalSettings = null
) : ICommand<MobileApplicationResponse>;
