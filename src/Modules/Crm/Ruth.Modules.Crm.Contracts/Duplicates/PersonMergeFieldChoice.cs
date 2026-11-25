namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Selects which person's value wins for a specific field during a side-by-side merge.</summary>
public enum PersonMergeFieldChoice
{
    /// <summary>Keep the surviving person's existing value.</summary>
    Surviving = 0,

    /// <summary>Copy the retired person's value onto the surviving person.</summary>
    Retired = 1,
}
