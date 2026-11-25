namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Returned once upon REST API key creation with the plaintext secret key.</summary>
public sealed record CreateRestApiKeyResponse(RestApiKeyResponse Key, string PlaintextApiKey);
