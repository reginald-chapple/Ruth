namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Represents a previous last name recorded for a person.</summary>
public sealed record PersonPreviousNameResponse(
    Guid Id,
    string LastName,
    DateTimeOffset CreatedOnUtc
);
