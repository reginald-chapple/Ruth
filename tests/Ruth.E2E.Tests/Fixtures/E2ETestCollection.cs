using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Ruth.E2E.Tests.Fixtures;

[CollectionDefinition(Name)]
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit collection definition fixture named per TEST_INFRA.md."
)]
public sealed class E2ETestCollection : ICollectionFixture<E2EContainersFixture>
{
    public const string Name = "Ruth.E2E";
}
