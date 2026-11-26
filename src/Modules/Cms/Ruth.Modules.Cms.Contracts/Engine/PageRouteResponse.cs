namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a friendly URL route mapped to a CMS page.</summary>
public sealed record PageRouteResponse(Guid Id, Guid PageId, string Route, bool IsGlobal);
