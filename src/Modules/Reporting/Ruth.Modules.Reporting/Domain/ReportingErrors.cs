using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Domain;

internal static class ReportingErrors
{
    public static DomainError Validation(string code, string description)
    {
        return DomainError.Validation($"Reporting.{code}", description);
    }

    public static DomainError NotFound(string code, string description)
    {
        return DomainError.NotFound($"Reporting.{code}", description);
    }

    public static DomainError Conflict(string code, string description)
    {
        return DomainError.Conflict($"Reporting.{code}", description);
    }
}
