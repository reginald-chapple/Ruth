using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Reporting.Domain.Reports;

/// <summary>Tabular Report aggregate combining a DataView with ordered IFieldSelectPlugin columns.</summary>
public sealed class Report
    : AggregateRoot<ReportId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Reporting.Report";

    private readonly List<ReportField> _fields = [];

    private Report() { }

    private Report(ReportId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string EntityTypeKey { get; private set; } = string.Empty;
    public DataViewId? DataViewId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public int? FetchTop { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<ReportField> Fields => _fields.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    string ISecured.EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<Report> Create(
        string name,
        string entityTypeKey,
        IReadOnlyList<ReportFieldSpec> fields,
        DataViewId? dataViewId = null,
        string? description = null,
        Guid? categoryId = null,
        int? fetchTop = null
    )
    {
        var validation = ValidateInputs(name, entityTypeKey, fields, fetchTop);
        if (validation.IsFailure)
        {
            return Result.Failure<Report>(validation.PrimaryError);
        }

        var report = new Report(ReportId.Create())
        {
            Name = name.Trim(),
            EntityTypeKey = entityTypeKey.Trim(),
            DataViewId = dataViewId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CategoryId = categoryId,
            FetchTop = fetchTop,
            IsActive = true,
        };

        for (var i = 0; i < fields.Count; i++)
        {
            var fieldRes = ReportField.Create(report.Id, fields[i], i);
            if (fieldRes.IsFailure)
            {
                return Result.Failure<Report>(fieldRes.PrimaryError);
            }

            report._fields.Add(fieldRes.Value);
        }

        return Result.Success(report);
    }

    private static Result ValidateInputs(
        string name,
        string entityTypeKey,
        IReadOnlyList<ReportFieldSpec> fields,
        int? fetchTop
    )
    {
        return string.IsNullOrWhiteSpace(name)
                ? Result.Failure(
                    ReportingErrors.Validation("Report.NameRequired", "Report name is required.")
                )
            : string.IsNullOrWhiteSpace(entityTypeKey)
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "Report.EntityTypeRequired",
                        "Report entity type key is required."
                    )
                )
            : fields is not { Count: > 0 }
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "Report.FieldsRequired",
                        "Report must define at least one column field."
                    )
                )
            : fetchTop is <= 0
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "Report.FetchTopInvalid",
                        "FetchTop must be greater than zero when specified."
                    )
                )
            : Result.Success();
    }
}
