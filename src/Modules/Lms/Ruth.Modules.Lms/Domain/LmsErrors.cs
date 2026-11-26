using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain;

internal static class LmsErrors
{
    public static DomainError Validation(string code, string description)
    {
        return DomainError.Validation($"Lms.{code}", description);
    }

    public static DomainError NotFound(string code, string description)
    {
        return DomainError.NotFound($"Lms.{code}", description);
    }

    public static DomainError Conflict(string code, string description)
    {
        return DomainError.Conflict($"Lms.{code}", description);
    }
}
