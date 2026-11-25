namespace Ruth.Modules.Core.Domain.FieldTypes;

internal static class BuiltInFieldTypes
{
    public static IReadOnlyList<IFieldTypePlugin> CreateAll()
    {
        return
        [
            new TextFieldType(),
            new MemoFieldType(),
            new BooleanFieldType(),
            new IntegerFieldType(),
            new DecimalFieldType(),
            new CurrencyFieldType(),
            new DateFieldType(),
            new DateTimeFieldType(),
            new DateRangeFieldType(),
            new TimeFieldType(),
            new IntegerRangeFieldType(),
            new DecimalRangeFieldType(),
            new SingleSelectFieldType(),
            new MultiSelectFieldType(),
            new DefinedValueFieldType(),
            new CategoryFieldType(),
            new KeyValueListFieldType(),
            new JsonFieldType(),
            new CodeFieldType(),
            new ColorFieldType(),
            new UrlFieldType(),
            new EmailFieldType(),
            new PhoneFieldType(),
            new RatingFieldType(),
            new MatrixFieldType(),
        ];
    }
}
