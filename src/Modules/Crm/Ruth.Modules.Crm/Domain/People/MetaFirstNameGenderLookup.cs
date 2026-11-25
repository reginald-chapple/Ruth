using Ruth.Modules.Crm.Contracts.People;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Read-only first-name statistical gender lookup entry.</summary>
public sealed class MetaFirstNameGenderLookup
{
    private MetaFirstNameGenderLookup() { }

    /// <summary>Creates a first-name gender lookup row.</summary>
    public MetaFirstNameGenderLookup(
        Guid id,
        string firstName,
        int maleCount,
        int femaleCount,
        string country = "US",
        string language = "en"
    )
    {
        Id = id;
        FirstName = firstName.Trim();
        NormalizedFirstName = FirstName.ToUpperInvariant();
        MaleCount = Math.Max(0, maleCount);
        FemaleCount = Math.Max(0, femaleCount);
        TotalCount = MaleCount + FemaleCount;
        MalePercent =
            TotalCount == 0
                ? 0m
                : Math.Round(
                    (decimal)MaleCount / TotalCount * 100m,
                    2,
                    MidpointRounding.AwayFromZero
                );
        FemalePercent =
            TotalCount == 0
                ? 0m
                : Math.Round(
                    (decimal)FemaleCount / TotalCount * 100m,
                    2,
                    MidpointRounding.AwayFromZero
                );
        Country = country;
        Language = language;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized first name.</summary>
    public string NormalizedFirstName { get; private set; } = string.Empty;

    /// <summary>Gets the observed male count.</summary>
    public int MaleCount { get; private set; }

    /// <summary>Gets the observed female count.</summary>
    public int FemaleCount { get; private set; }

    /// <summary>Gets the total observed count.</summary>
    public int TotalCount { get; private set; }

    /// <summary>Gets the male percentage (0–100).</summary>
    public decimal MalePercent { get; private set; }

    /// <summary>Gets the female percentage (0–100).</summary>
    public decimal FemalePercent { get; private set; }

    /// <summary>Gets the ISO country code.</summary>
    public string Country { get; private set; } = "US";

    /// <summary>Gets the ISO language code.</summary>
    public string Language { get; private set; } = "en";

    /// <summary>Predicts gender when one side holds at least 60% probability.</summary>
    public Gender PredictGender()
    {
        return MalePercent >= 60m ? Gender.Male
            : FemalePercent >= 60m ? Gender.Female
            : Gender.Unknown;
    }
}
