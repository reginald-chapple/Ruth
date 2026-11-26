using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Command to create a CMS media account (Vimeo, YouTube, HLS, or in-memory).</summary>
public sealed record CreateMediaAccountCommand(
    string Name,
    string Slug,
    string ProviderKey,
    string AccountIdentifier,
    IReadOnlyDictionary<string, string?>? Settings = null,
    bool IsActive = true
) : ICommand<MediaAccountResponse>;
