using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.Tests.Fixtures;

public readonly record struct TestWidgetId(Guid Value) : IStronglyTypedId<TestWidgetId>
{
    public static TestWidgetId Empty => new(Guid.Empty);

    public static TestWidgetId Create()
    {
        return new TestWidgetId(StronglyTypedId.NextGuid());
    }

    public static TestWidgetId From(Guid value)
    {
        return new TestWidgetId(value);
    }
}
