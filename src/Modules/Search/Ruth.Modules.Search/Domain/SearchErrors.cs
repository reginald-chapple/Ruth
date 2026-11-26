using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Search.Domain;

internal static class SearchErrors
{
    public static DomainError Validation(string code, string description)
    {
        return DomainError.Validation($"Search.{code}", description);
    }

    public static DomainError NotFound(string code, string description)
    {
        return DomainError.NotFound($"Search.{code}", description);
    }

    public static DomainError Conflict(string code, string description)
    {
        return DomainError.Conflict($"Search.{code}", description);
    }
}
