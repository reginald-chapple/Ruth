using Ruth.Modules.Cms.Contracts.Engine;

namespace Ruth.Host.Components.Cms;

/// <summary>Descriptor pairing a CMS block type key with its compiled Blazor component implementing <see cref="IRuthBlock" />.</summary>
public sealed record RuthBlockDescriptor(
    string BlockKey,
    string DisplayName,
    string Category,
    bool RequiresInteractivity,
    Type ComponentType
);
