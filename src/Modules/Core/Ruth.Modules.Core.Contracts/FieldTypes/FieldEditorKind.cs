namespace Ruth.Modules.Core.Contracts.FieldTypes;

/// <summary>UI editor hint returned by a field type plugin to drive dynamic Blazor inputs.</summary>
public enum FieldEditorKind
{
    /// <summary>Single-line text input.</summary>
    Text = 1,

    /// <summary>Multi-line text area.</summary>
    MultilineText = 2,

    /// <summary>Boolean checkbox or switch.</summary>
    Boolean = 3,

    /// <summary>Whole number input.</summary>
    WholeNumber = 4,

    /// <summary>Decimal numeric input.</summary>
    DecimalNumber = 5,

    /// <summary>Currency amount input.</summary>
    Currency = 6,

    /// <summary>Calendar date input.</summary>
    Date = 7,

    /// <summary>Date and time input.</summary>
    DateTime = 8,

    /// <summary>Start and end date range inputs.</summary>
    DateRange = 9,

    /// <summary>Time-of-day input.</summary>
    Time = 10,

    /// <summary>Lower and upper integer range inputs.</summary>
    IntegerRange = 11,

    /// <summary>Lower and upper decimal range inputs.</summary>
    DecimalRange = 12,

    /// <summary>Single option dropdown selector.</summary>
    SingleSelect = 13,

    /// <summary>Multiple option checkbox or list selector.</summary>
    MultiSelect = 14,

    /// <summary>DefinedValue lookup selector.</summary>
    DefinedValue = 15,

    /// <summary>Category selector.</summary>
    Category = 16,

    /// <summary>Dynamic key/value pair list editor.</summary>
    KeyValueList = 17,

    /// <summary>Structured JSON editor.</summary>
    Json = 18,

    /// <summary>Source code / template editor.</summary>
    Code = 19,

    /// <summary>Hex color picker.</summary>
    Color = 20,

    /// <summary>URL input.</summary>
    Url = 21,

    /// <summary>Email address input.</summary>
    Email = 22,

    /// <summary>Telephone number input.</summary>
    Phone = 23,

    /// <summary>Numeric star rating selector.</summary>
    Rating = 24,

    /// <summary>Repeatable structured attribute matrix table.</summary>
    Matrix = 25,
}
