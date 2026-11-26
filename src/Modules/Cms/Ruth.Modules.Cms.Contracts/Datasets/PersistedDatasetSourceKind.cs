namespace Ruth.Modules.Cms.Contracts.Datasets;

/// <summary>Built-in declarative data source kind powering a PersistedDataset.</summary>
public enum PersistedDatasetSourceKind
{
    /// <summary>Static JSON payload authored directly in configuration.</summary>
    StaticJson = 0,

    /// <summary>Materialized published items from a CMS ContentChannel.</summary>
    ContentChannelFeed = 1,

    /// <summary>Materialized navigation/page summary from a CMS Site.</summary>
    SitePagesSummary = 2,

    /// <summary>Materialized active AdaptiveMessage catalog summary.</summary>
    AdaptiveMessagesSummary = 3,

    /// <summary>Materialized media elements from a CMS MediaAccount or MediaFolder.</summary>
    MediaElementsFeed = 4,
}
