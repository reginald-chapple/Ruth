namespace Ruth.Modules.Search.Domain;

/// <summary>Deterministic well-known system GUIDs owned by the Search module.</summary>
public static class SearchSystemGuids
{
    public static readonly Guid ContentCollection = new(
        0x70000004,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x01
    );

    public static readonly Guid SearchDocument = new(
        0x70000004,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x02
    );
}
