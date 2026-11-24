namespace Ruth.E2E.Tests.Fixtures;

public sealed record ColumnSchemaInfo(
    string ColumnName,
    string DataType,
    string UdtName,
    bool IsNullable
);
