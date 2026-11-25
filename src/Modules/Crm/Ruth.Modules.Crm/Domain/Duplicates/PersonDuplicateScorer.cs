using Ruth.Modules.Crm.Contracts.People;

namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>
/// Pure domain scoring engine implementing Rock RMS duplicate weights (TotalCapacity = 29)
/// and geometric-mean ConfidenceScore (0–100) with nickname/metaphone awareness.
/// </summary>
public static class PersonDuplicateScorer
{
    /// <summary>Rock RMS maximum weighted capacity across all scored demographic fields.</summary>
    public const int TotalCapacity = 29;

    /// <summary>Evaluates duplicate likelihood between <paramref name="left" /> and <paramref name="right" />.</summary>
    public static DuplicateScoreEvaluation Evaluate(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var tags = new List<string>(12);
        var primaryScore = ScorePrimarySignals(left, right, tags);
        if (primaryScore == 0)
        {
            return new DuplicateScoreEvaluation(
                IsCandidateMatch: false,
                Score: 0,
                Capacity: 0,
                TotalCapacity,
                ConfidenceScore: 0m,
                string.Empty
            );
        }

        var secondaryScore = ScoreSecondarySignals(left, right, tags);
        var totalScore = primaryScore + secondaryScore;
        var capacity = ComputeCapacity(left);
        var confidence = ComputeConfidenceScore(totalScore, capacity, TotalCapacity);

        return new DuplicateScoreEvaluation(
            IsCandidateMatch: true,
            totalScore,
            capacity,
            TotalCapacity,
            confidence,
            string.Join('|', tags)
        );
    }

    /// <summary>Computes the Rock RMS geometric-mean confidence percentage (0–100).</summary>
    public static decimal ComputeConfidenceScore(int score, int capacity, int totalCapacity)
    {
        if (score <= 0 || capacity <= 0 || totalCapacity <= 0)
        {
            return 0m;
        }

        var capacityRatio = capacity / (totalCapacity * 0.01d);
        var scoreRatio = score / (capacity * 0.01d);
        var geometricMean = Math.Sqrt(capacityRatio * scoreRatio);
        return Math.Round((decimal)Math.Min(100d, geometricMean), 2, MidpointRounding.AwayFromZero);
    }

    private static int ScorePrimarySignals(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = 0;

        if (MatchesEmail(left, right))
        {
            score += 4;
            tags.Add("Email");
        }

        if (MatchesPartialName(left, right))
        {
            score += 1;
            tags.Add("PartialName");
        }

        var differentHousehold = AreDifferentHouseholds(left, right);
        if (
            differentHousehold
            && left.Gender == right.Gender
            && SharesAny(left.CellPhoneKeys, right.CellPhoneKeys)
        )
        {
            score += 4;
            tags.Add("CellPhone");
        }

        if (
            differentHousehold
            && left.Gender == right.Gender
            && SharesAny(left.HomePhoneKeys, right.HomePhoneKeys)
        )
        {
            score += 2;
            tags.Add("Phone");
        }

        if (
            differentHousehold
            && left.PrimaryHomeLocationId.HasValue
            && left.PrimaryHomeLocationId == right.PrimaryHomeLocationId
            && left.PrimaryHouseholdRole == right.PrimaryHouseholdRole
        )
        {
            score += 2;
            tags.Add("Address");
        }

        return score;
    }

    private static int ScoreSecondarySignals(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = 0;
        if (MatchesFullFirstNameOrNickname(left, right))
        {
            score += 3;
            tags.Add("FirstName");
        }

        if (
            !string.IsNullOrWhiteSpace(left.LastName)
            && string.Equals(left.LastName, right.LastName, StringComparison.OrdinalIgnoreCase)
        )
        {
            score += 3;
            tags.Add("LastName");
        }

        if (left.BirthDate.HasValue && left.BirthDate == right.BirthDate)
        {
            score += 3;
            tags.Add("Birthdate");
        }

        score += ScoreDemographicTies(left, right, tags);
        return score;
    }

    private static int ScoreDemographicTies(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = 0;
        if (left.Gender == right.Gender)
        {
            score += 1;
            tags.Add("Gender");
        }

        if (left.PrimaryCampusId.HasValue && left.PrimaryCampusId == right.PrimaryCampusId)
        {
            score += 1;
            tags.Add("Campus");
        }

        if (
            left.MaritalStatus != MaritalStatus.Unknown
            && left.MaritalStatus == right.MaritalStatus
        )
        {
            score += 1;
            tags.Add("Marital");
        }

        if (
            !string.IsNullOrWhiteSpace(left.Suffix)
            && string.Equals(left.Suffix, right.Suffix, StringComparison.OrdinalIgnoreCase)
        )
        {
            score += 4;
            tags.Add("Suffix");
        }

        return score;
    }

    private static int ComputeCapacity(DuplicateCandidateProfile profile)
    {
        var capacity = 2; // Gender (1) + MaritalStatus (1) baseline per Rock RMS
        if (!string.IsNullOrWhiteSpace(profile.NormalizedEmail))
        {
            capacity += 4;
        }

        if (profile.FirstName.Length >= 2)
        {
            capacity += 1;
        }

        if (
            !string.IsNullOrWhiteSpace(profile.FirstName)
            || !string.IsNullOrWhiteSpace(profile.NickName)
        )
        {
            capacity += 3;
        }

        if (!string.IsNullOrWhiteSpace(profile.LastName))
        {
            capacity += 3;
        }

        if (profile.BirthDate.HasValue)
        {
            capacity += 3;
        }

        if (!string.IsNullOrWhiteSpace(profile.Suffix))
        {
            capacity += 4;
        }

        if (profile.HomePhoneKeys.Count > 0)
        {
            capacity += 2;
        }

        if (profile.CellPhoneKeys.Count > 0)
        {
            capacity += 4;
        }

        if (profile.PrimaryHomeLocationId.HasValue)
        {
            capacity += 2;
        }

        if (profile.PrimaryCampusId.HasValue)
        {
            capacity += 1;
        }

        return Math.Min(TotalCapacity, capacity);
    }

    private static bool MatchesEmail(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        return !string.IsNullOrWhiteSpace(left.NormalizedEmail)
            && string.Equals(left.NormalizedEmail, right.NormalizedEmail, StringComparison.Ordinal);
    }

    private static bool MatchesPartialName(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        var sameLastOrPhonetic =
            !string.IsNullOrWhiteSpace(left.LastName)
            && (
                string.Equals(left.LastName, right.LastName, StringComparison.OrdinalIgnoreCase)
                || (
                    !string.IsNullOrWhiteSpace(left.LastNameMetaphone)
                    && string.Equals(
                        left.LastNameMetaphone,
                        right.LastNameMetaphone,
                        StringComparison.Ordinal
                    )
                )
            );
        if (!sameLastOrPhonetic)
        {
            return false;
        }

        var prefixMatch =
            left.FirstName.Length >= 2
            && right.FirstName.Length >= 2
            && left.FirstName.AsSpan(0, 2)
                .Equals(right.FirstName.AsSpan(0, 2), StringComparison.OrdinalIgnoreCase);

        var metaphoneMatch =
            !string.IsNullOrWhiteSpace(left.FirstNameMetaphone)
            && string.Equals(
                left.FirstNameMetaphone,
                right.FirstNameMetaphone,
                StringComparison.Ordinal
            );

        return prefixMatch
            || metaphoneMatch
            || SharesAny(left.ExpandedFirstNames, right.ExpandedFirstNames);
    }

    private static bool MatchesFullFirstNameOrNickname(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        return SharesAny(left.ExpandedFirstNames, right.ExpandedFirstNames)
            || MatchesAnyPair(left.FirstName, left.NickName, right.FirstName, right.NickName);
    }

    private static bool MatchesAnyPair(string a1, string a2, string b1, string b2)
    {
        return (!string.IsNullOrWhiteSpace(a1) && (EqualsCI(a1, b1) || EqualsCI(a1, b2)))
            || (!string.IsNullOrWhiteSpace(a2) && (EqualsCI(a2, b1) || EqualsCI(a2, b2)));
    }

    private static bool EqualsCI(string left, string right)
    {
        return !string.IsNullOrWhiteSpace(right)
            && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreDifferentHouseholds(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        return !left.PrimaryHouseholdId.HasValue
            || !right.PrimaryHouseholdId.HasValue
            || left.PrimaryHouseholdId != right.PrimaryHouseholdId;
    }

    private static bool SharesAny(IEnumerable<string> left, IEnumerable<string> right)
    {
        foreach (var item in left)
        {
            if (right.Contains(item, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
