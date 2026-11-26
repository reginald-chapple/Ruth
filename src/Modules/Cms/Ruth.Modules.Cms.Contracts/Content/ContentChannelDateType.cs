namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Date range policy applied to items inside a ContentChannelType.</summary>
public enum ContentChannelDateType
{
    /// <summary>Each item carries a single publication timestamp.</summary>
    SingleDate = 0,

    /// <summary>Each item carries both a start and expiration timestamp.</summary>
    DateRange = 1,

    /// <summary>Items are undated evergreen content.</summary>
    NoDates = 2,
}
