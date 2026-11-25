namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Represents an alternate lookup key associated with a person.</summary>
public sealed record PersonSearchKeyResponse(
    Guid Id,
    PersonSearchKeyType KeyType,
    Guid? SearchTypeValueId,
    string SearchValue,
    bool IsValuePrivate
);
