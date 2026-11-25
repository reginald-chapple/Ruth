using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

internal static class LocationTreeValidator
{
    public static Result ValidateParentMove(
        LocationId locationId,
        LocationId? candidateParentId,
        IReadOnlyDictionary<LocationId, LocationId?> parentLookup
    )
    {
        if (!candidateParentId.HasValue)
        {
            return Result.Success();
        }

        if (candidateParentId.Value == locationId)
        {
            return CoreLocationErrors.LocationCycleDetected;
        }

        var visited = new HashSet<LocationId> { locationId };
        LocationId? current = candidateParentId.Value;

        while (current.HasValue)
        {
            if (!visited.Add(current.Value))
            {
                return CoreLocationErrors.LocationCycleDetected;
            }

            if (!parentLookup.TryGetValue(current.Value, out current))
            {
                break;
            }
        }

        return Result.Success();
    }
}
