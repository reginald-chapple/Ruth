using Ruth.Persistence.EntityFramework;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

public sealed class SchemaValidatorTests
{
    [Theory]
    [InlineData("identity")]
    [InlineData("core")]
    [InlineData("scheduler_v1")]
    [InlineData("_internal")]
    public void Validate_Accepts_Valid_Postgres_Identifiers(string schema)
    {
        Assert.Equal(schema, SchemaValidator.Validate(schema));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1leading_digit")]
    [InlineData("UpperCase")]
    [InlineData("bad-hyphen")]
    [InlineData("alpha; DROP TABLE outbox_messages;--")]
    public void Validate_Rejects_Invalid_Or_Unsafe_Schemas(string schema)
    {
        _ = Assert.Throws<ArgumentException>(() => SchemaValidator.Validate(schema));
    }
}
