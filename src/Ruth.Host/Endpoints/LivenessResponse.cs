namespace Ruth.Host.Endpoints;

/// <summary>Represents the host liveness probe payload.</summary>
/// <param name="Status">Current liveness status string.</param>
public sealed record LivenessResponse(string Status);
