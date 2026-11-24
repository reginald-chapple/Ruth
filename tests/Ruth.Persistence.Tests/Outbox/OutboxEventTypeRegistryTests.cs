using Microsoft.EntityFrameworkCore;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Xunit;

namespace Ruth.Persistence.Tests.Outbox;

public sealed class OutboxEventTypeRegistryTests
{
    private sealed record SampleWidgetCreatedIntegrationEvent(string Name) : IntegrationEvent;

    private sealed record AnotherIntegrationEvent(int Count) : IntegrationEvent;

    private sealed record OpenGenericIntegrationEvent<T>(T Value) : IntegrationEvent;

    [Fact]
    public void Register_And_Resolve_Supports_Aliases_And_Assembly_Scan()
    {
        var registry = new OutboxEventTypeRegistry();
        _ = registry.Register<SampleWidgetCreatedIntegrationEvent>("sample.widget.created");
        _ = registry.RegisterFromAssembly(typeof(OutboxEventTypeRegistryTests).Assembly);

        Assert.Equal(
            "sample.widget.created",
            registry.GetEventTypeName(typeof(SampleWidgetCreatedIntegrationEvent))
        );
        Assert.Equal(
            typeof(SampleWidgetCreatedIntegrationEvent),
            registry.Resolve("sample.widget.created")
        );
        Assert.Equal(
            typeof(AnotherIntegrationEvent),
            registry.Resolve(
                OutboxEventTypeRegistry.GetCanonicalTypeName(typeof(AnotherIntegrationEvent))
            )
        );
    }

    [Fact]
    public void Register_Rejects_Duplicate_Key_For_Different_Type()
    {
        var registry = new OutboxEventTypeRegistry();
        _ = registry.Register<SampleWidgetCreatedIntegrationEvent>("duplicate.key");

        _ = Assert.Throws<InvalidOperationException>(() =>
            registry.Register<AnotherIntegrationEvent>("duplicate.key")
        );
    }

    [Fact]
    public void Resolve_Rejects_Unknown_Or_Non_Ruth_Types()
    {
        var registry = new OutboxEventTypeRegistry();

        Assert.Null(registry.Resolve(""));
        Assert.Null(registry.Resolve("System.String, System.Private.CoreLib"));
        Assert.Null(registry.Resolve("Ruth.NonExistent.FakeIntegrationEvent"));
    }

    [Theory]
    [InlineData("Foo, Version=notaversion")]
    [InlineData("Foo, PublicKeyToken=nothex")]
    [InlineData("A, B, C, D")]
    [InlineData("Foo[[Broken")]
    public void Resolve_Returns_Null_For_Malformed_Assembly_Qualified_Names(string malformedName)
    {
        var registry = new OutboxEventTypeRegistry();

        Assert.Null(registry.Resolve(malformedName));
    }

    [Fact]
    public void Register_And_AssemblyScan_Reject_Open_Generic_Integration_Events()
    {
        var registry = new OutboxEventTypeRegistry();

        _ = registry.RegisterFromAssembly(typeof(OutboxEventTypeRegistryTests).Assembly);
        _ = Assert.Throws<ArgumentException>(() =>
            registry.Register(typeof(OpenGenericIntegrationEvent<>))
        );
        Assert.Null(
            registry.Resolve(
                OutboxEventTypeRegistry.GetCanonicalTypeName(typeof(OpenGenericIntegrationEvent<>))
            )
        );
    }

    [Fact]
    public void EnqueueIntegrationEvent_Without_ApplicationServiceProvider_Uses_Canonical_Type_Name()
    {
        var options = new DbContextOptionsBuilder<AlphaModuleDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;
        using var db = new AlphaModuleDbContext(options);
        var evt = new TestWidgetIntegrationEvent(Guid.CreateVersion7(), "NoDi");

        db.EnqueueIntegrationEvent(evt);

        var staged = Assert.Single(db.OutboxMessages.Local);
        Assert.Equal(
            OutboxEventTypeRegistry.GetCanonicalTypeName(typeof(TestWidgetIntegrationEvent)),
            staged.EventType
        );
    }
}
