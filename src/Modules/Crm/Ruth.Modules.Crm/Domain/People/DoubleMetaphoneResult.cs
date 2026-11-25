namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Primary and optional alternate 4-character Double Metaphone keys.</summary>
public readonly record struct DoubleMetaphoneResult(string PrimaryKey, string? AlternateKey);
