using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Normalized node in a persisted DataView Query AST.</summary>
public sealed class DataViewFilter : Entity<DataViewFilterId>
{
    private DataViewFilter() { }

    private DataViewFilter(DataViewFilterId id)
        : base(id) { }

    public DataViewId DataViewId { get; private set; }
    public DataViewFilterId? ParentFilterId { get; private set; }
    public DataViewFilterExpressionType ExpressionType { get; private set; }
    public DataViewFilterRuleKind RuleKind { get; private set; }
    public string? FieldKey { get; private set; }
    public DataViewFilterOperator Operator { get; private set; }
    public string? Value { get; private set; }
    public string? SecondaryValue { get; private set; }
    public int Order { get; private set; }

    public static DataViewFilter Create(
        DataViewId dataViewId,
        DataViewFilterId? parentFilterId,
        DataViewFilterExpressionType expressionType,
        DataViewFilterRuleKind ruleKind,
        string? fieldKey,
        DataViewFilterOperator op,
        string? value,
        string? secondaryValue,
        int order
    )
    {
        return new DataViewFilter(DataViewFilterId.Create())
        {
            DataViewId = dataViewId,
            ParentFilterId = parentFilterId,
            ExpressionType = expressionType,
            RuleKind = ruleKind,
            FieldKey = string.IsNullOrWhiteSpace(fieldKey) ? null : fieldKey.Trim(),
            Operator = op,
            Value = value,
            SecondaryValue = secondaryValue,
            Order = order,
        };
    }
}
