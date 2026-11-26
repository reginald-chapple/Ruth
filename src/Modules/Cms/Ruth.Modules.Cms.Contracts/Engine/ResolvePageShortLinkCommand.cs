using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to resolve a shortlink token, increment click count, and return its target URL.</summary>
public sealed record ResolvePageShortLinkCommand(string Token, string? Host = null)
    : ICommand<PageShortLinkResponse>;
