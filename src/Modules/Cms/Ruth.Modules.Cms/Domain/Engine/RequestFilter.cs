using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Declarative request filter evaluated against incoming HTTP context for personalization and routing.</summary>
public sealed class RequestFilter
    : AggregateRoot<RequestFilterId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private List<RequestFilterRuleSpec> _rules = [];

    private RequestFilter() { }

    private RequestFilter(RequestFilterId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public CmsSiteId? SiteId { get; private set; }
    public RequestFilterMode FilterMode { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<RequestFilterRuleSpec> Rules
    {
        get => _rules;
        private set => _rules = [.. value];
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    public static Result<RequestFilter> Create(
        string name,
        string key,
        CmsSiteId? siteId = null,
        RequestFilterMode filterMode = RequestFilterMode.AllMatch,
        IReadOnlyList<RequestFilterRuleSpec>? rules = null,
        bool isActive = true
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(key);
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            return Result.Failure<RequestFilter>(
                CmsErrors.Validation("RequestFilter.Invalid", "Filter name and key are required.")
            );
        }

        List<RequestFilterRuleSpec> normalizedRules = rules is null ? [] : [.. rules];

        return Result.Success(
            new RequestFilter(RequestFilterId.Create())
            {
                Name = name.Trim(),
                Key = normalizedKey,
                SiteId = siteId,
                FilterMode = filterMode,
                _rules = normalizedRules,
                IsActive = isActive,
            }
        );
    }
}
