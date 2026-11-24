using Ruth.SharedKernel.Primitives;
using Xunit;

namespace Ruth.SharedKernel.Tests.Primitives;

public sealed class StronglyTypedIdTests
{
    private readonly record struct SampleId(Guid Value) : IStronglyTypedId<SampleId>
    {
        public static SampleId Empty => new(Guid.Empty);

        public static SampleId Create()
        {
            return new SampleId(StronglyTypedId.NextGuid());
        }

        public static SampleId From(Guid value)
        {
            return new SampleId(value);
        }
    }

    [Fact]
    public void Create_Generates_NonEmpty_UuidV7()
    {
        var id = StronglyTypedId.Create<SampleId>();

        Assert.NotEqual(Guid.Empty, id.Value);
        Assert.Equal(7, id.Value.Version);
        Assert.False(((IStronglyTypedId<SampleId>)id).IsEmpty);
    }

    [Fact]
    public void Create_WithTimestamp_Produces_TimeOrdered_Ids()
    {
        var earlier = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var later = earlier.AddSeconds(5);

        var first = StronglyTypedId.Create<SampleId>(earlier);
        var second = StronglyTypedId.Create<SampleId>(later);

        Assert.Equal(7, first.Value.Version);
        Assert.Equal(7, second.Value.Version);
        Assert.True(StronglyTypedId.Compare(first, second) < 0);
        Assert.Equal(7, StronglyTypedId.NextGuid(earlier).Version);
    }

    [Fact]
    public void Empty_Reports_IsEmpty_True()
    {
        IStronglyTypedId<SampleId> empty = SampleId.Empty;

        Assert.Equal(Guid.Empty, empty.Value);
        Assert.True(empty.IsEmpty);
    }

    [Fact]
    public void TryParse_Span_And_String_Handles_Valid_Invalid_And_Null()
    {
        var original = SampleId.Create();
        var text = original.Value.ToString();

        var parsedFromSpan = StronglyTypedId.TryParse(text.AsSpan(), out SampleId spanId);
        var parsedFromString = StronglyTypedId.TryParse(text, out SampleId stringId);
        var parsedInvalid = StronglyTypedId.TryParse("not-a-guid".AsSpan(), out SampleId invalidId);
        string? nullInput = null;
        var parsedNull = StronglyTypedId.TryParse(nullInput, out SampleId nullId);

        Assert.True(parsedFromSpan);
        Assert.Equal(original, spanId);
        Assert.True(parsedFromString);
        Assert.Equal(original, stringId);
        Assert.False(parsedInvalid);
        Assert.Equal(SampleId.Empty, invalidId);
        Assert.False(parsedNull);
        Assert.Equal(SampleId.Empty, nullId);
    }
}
