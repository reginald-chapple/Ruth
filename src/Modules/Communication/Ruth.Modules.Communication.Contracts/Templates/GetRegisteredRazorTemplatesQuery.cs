using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Lists all compile-time registered Razor component templates.</summary>
public sealed record GetRegisteredRazorTemplatesQuery(string? Category = null)
    : IQuery<IReadOnlyList<RazorTemplateSummaryResponse>>;
