using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Lists electronic signature document templates.</summary>
public sealed record GetSignatureDocumentTemplatesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<SignatureDocumentTemplateResponse>>;
