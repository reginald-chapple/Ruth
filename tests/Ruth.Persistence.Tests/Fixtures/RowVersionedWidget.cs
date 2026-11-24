using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed class RowVersionedWidget : Entity<TestWidgetId>
{
    private RowVersionedWidget() { }

    public RowVersionedWidget(TestWidgetId id, string label)
        : base(id)
    {
        Label = label;
    }

    public string Label { get; private set; } = string.Empty;

    public void ChangeLabel(string newLabel)
    {
        Label = newLabel;
    }
}
