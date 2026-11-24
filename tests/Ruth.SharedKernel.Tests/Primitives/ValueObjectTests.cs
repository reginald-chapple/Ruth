using Ruth.SharedKernel.Primitives;
using Xunit;

namespace Ruth.SharedKernel.Tests.Primitives;

public sealed class ValueObjectTests
{
    private sealed class MailingAddress(string street, string? line2, string city) : ValueObject
    {
        public string Street { get; } = street;

        public string? Line2 { get; } = line2;

        public string City { get; } = city;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return Line2;
            yield return City;
        }
    }

    private sealed class AlternateAddress(string street, string? line2, string city) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return street;
            yield return line2;
            yield return city;
        }
    }

    [Fact]
    public void ValueObject_Uses_Structural_Equality_And_HashCode()
    {
        var first = new MailingAddress("123 Main St", line2: null, "Phoenix");
        var second = new MailingAddress("123 Main St", line2: null, "Phoenix");
        var different = new MailingAddress("123 Main St", "Apt 4", "Phoenix");
        var differentType = new AlternateAddress("123 Main St", line2: null, "Phoenix");

        Assert.True(first.Equals(first));
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        Assert.False(first.Equals(different));
        Assert.True(first != different);
        Assert.False(first.Equals(differentType));
        Assert.False(first.Equals(other: null));
        Assert.False(first.Equals("not-a-value-object"));
    }
}
