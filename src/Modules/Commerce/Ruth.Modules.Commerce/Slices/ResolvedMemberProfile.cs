namespace Ruth.Modules.Commerce.Slices;

internal sealed record ResolvedMemberProfile(
    IReadOnlySet<string> Tiers,
    IReadOnlySet<Guid> GroupIds,
    IReadOnlySet<Guid> GroupTypeIds
);
