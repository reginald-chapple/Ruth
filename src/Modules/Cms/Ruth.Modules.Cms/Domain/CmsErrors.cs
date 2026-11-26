using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Canonical domain errors for the Cms bounded context.</summary>
public static class CmsErrors
{
    /// <summary>Creates a validation domain error.</summary>
    public static DomainError Validation(string code, string description)
    {
        return DomainError.Validation($"Cms.{code}", description);
    }

    /// <summary>Creates a not-found domain error.</summary>
    public static DomainError NotFound(string entity, string identifier)
    {
        return DomainError.NotFound(
            $"Cms.{entity}.NotFound",
            $"{entity} '{identifier}' was not found."
        );
    }

    /// <summary>Creates a conflict domain error.</summary>
    public static DomainError Conflict(string code, string description)
    {
        return DomainError.Conflict($"Cms.{code}", description);
    }

    /// <summary>Creates a forbidden domain error.</summary>
    public static DomainError Forbidden(string code, string description)
    {
        return DomainError.Forbidden($"Cms.{code}", description);
    }
}
