using Ruth.Host.Components.Cms.Blocks;

namespace Ruth.Host.Components.Cms;

/// <summary>Compile-time registry mapping CMS block keys to Blazor DynamicComponent types implementing IRuthBlock.</summary>
public sealed class RuthBlockComponentRegistry
{
    private readonly Dictionary<string, RuthBlockDescriptor> _descriptors = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["cms.html-content"] = new(
            "cms.html-content",
            "HTML Content",
            "CMS",
            RequiresInteractivity: false,
            typeof(HtmlContentCmsBlock)
        ),
        ["cms.channel-feed"] = new(
            "cms.channel-feed",
            "Content Channel List",
            "CMS",
            RequiresInteractivity: false,
            typeof(ContentChannelListCmsBlock)
        ),
        ["cms.persisted-dataset"] = new(
            "cms.persisted-dataset",
            "Persisted Dataset View",
            "CMS",
            RequiresInteractivity: false,
            typeof(PersistedDatasetCmsBlock)
        ),
        ["cms.adaptive-message"] = new(
            "cms.adaptive-message",
            "Adaptive Message Banner",
            "Personalization",
            RequiresInteractivity: true,
            typeof(AdaptiveMessageCmsBlock)
        ),
        ["cms.media-gallery"] = new(
            "cms.media-gallery",
            "Media Gallery",
            "Media",
            RequiresInteractivity: true,
            typeof(MediaGalleryCmsBlock)
        ),
    };

    /// <summary>Gets all registered CMS block component descriptors.</summary>
    public IReadOnlyCollection<RuthBlockDescriptor> GetAll()
    {
        return _descriptors.Values;
    }

    /// <summary>Tries to resolve a compiled Blazor component descriptor by block type key.</summary>
    public bool TryGetDescriptor(string blockKey, out RuthBlockDescriptor? descriptor)
    {
        return _descriptors.TryGetValue(blockKey, out descriptor);
    }
}
