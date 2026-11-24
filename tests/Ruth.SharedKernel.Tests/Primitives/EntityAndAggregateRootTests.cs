using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;
using Xunit;

namespace Ruth.SharedKernel.Tests.Primitives;

public sealed class EntityAndAggregateRootTests
{
    private readonly record struct WidgetId(Guid Value) : IStronglyTypedId<WidgetId>
    {
        public static WidgetId Empty => new(Guid.Empty);

        public static WidgetId Create()
        {
            return new WidgetId(StronglyTypedId.NextGuid());
        }

        public static WidgetId From(Guid value)
        {
            return new WidgetId(value);
        }
    }

    private sealed record WidgetRenamedEvent(WidgetId Id, string NewName) : DomainEvent;

    private sealed class WidgetAggregate : AggregateRoot<WidgetId>
    {
        public WidgetAggregate() { }

        public WidgetAggregate(WidgetId id, string name)
            : base(id)
        {
            Name = name;
        }

        public string Name { get; private set; } = string.Empty;

        public void Rename(string newName)
        {
            Name = newName;
            RaiseDomainEvent(new WidgetRenamedEvent(Id, newName));
        }

        public void RaiseNull()
        {
            RaiseDomainEvent(null!);
        }
    }

    private sealed class OtherWidgetEntity(WidgetId id) : Entity<WidgetId>(id);

    [Fact]
    public void Entity_Constructor_Rejects_Default_Id()
    {
        _ = Assert.Throws<ArgumentException>(() => new WidgetAggregate(default, "Invalid"));
    }

    [Fact]
    public void Entity_Equality_Compares_Type_And_Identity()
    {
        var id = WidgetId.Create();
        var left = new WidgetAggregate(id, "Alpha");
        var right = new WidgetAggregate(id, "Beta");
        var differentId = new WidgetAggregate(WidgetId.Create(), "Alpha");
        var differentType = new OtherWidgetEntity(id);
        Entity<WidgetId> leftBase = left;
        Entity<WidgetId> rightBase = right;

        Assert.True(left.Equals(left));
        Assert.True(left.Equals(right));
        Assert.True(left.Equals((object)right));
        Assert.True(leftBase == rightBase);
        Assert.False(leftBase != rightBase);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());

        Assert.False(left.Equals(differentId));
        Assert.False(left.Equals(differentType));
        Assert.False(left.Equals(other: null));
        Assert.False(left.Equals("not-an-entity"));
        Assert.True(leftBase != differentId);
    }

    [Fact]
    public void Transient_Entities_Are_Not_Equal_Unless_Same_Reference()
    {
        var first = new WidgetAggregate();
        var second = new WidgetAggregate();

        Assert.True(first.IsTransient);
        Assert.True(first.Equals(first));
        Assert.False(first.Equals(second));
        Assert.NotEqual(0, first.GetHashCode());
    }

    [Fact]
    public void AggregateRoot_Raises_And_Clears_DomainEvents()
    {
        var aggregate = new WidgetAggregate(WidgetId.Create(), "Initial");

        Assert.Empty(aggregate.DomainEvents);

        aggregate.Rename("First");
        aggregate.Rename("Second");

        Assert.Equal(2, aggregate.DomainEvents.Count);
        var events = aggregate.DomainEvents.OfType<WidgetRenamedEvent>().ToArray();
        Assert.Equal("First", events[0].NewName);
        Assert.Equal("Second", events[1].NewName);

        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AggregateRoot_Rejects_Null_DomainEvent()
    {
        var aggregate = new WidgetAggregate(WidgetId.Create(), "Initial");

        _ = Assert.Throws<ArgumentNullException>(aggregate.RaiseNull);
    }
}
