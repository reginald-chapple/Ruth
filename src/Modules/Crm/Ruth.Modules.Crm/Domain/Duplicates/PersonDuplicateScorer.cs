namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>
/// Pure domain record-linkage scorer evaluating pairwise identity similarity across anchor identifiers
/// and corroborating demographics on a 100-point completeness-adjusted scale, including suffix collision penalties.
/// </summary>
public static class PersonDuplicateScorer
{
    /// <summary>Maximum possible linkage weight across all identity attributes.</summary>
    public const int MaxPossibleWeight = 100;

    private const int EmailWeight = 30;
    private const int MobilePhoneWeight = 20;
    private const int LandlinePhoneWeight = 8;
    private const int HomeAddressWeight = 7;
    private const int NameAnchorWeight = 5;
    private const int FirstNameWeight = 10;
    private const int LastNameWeight = 10;
    private const int BirthDateWeight = 15;
    private const int BranchWeight = 5;
    private const int SuffixMatchWeight = 5;
    private const int SuffixConflictPenalty = 35;

    /// <summary>Evaluates duplicate likelihood between <paramref name="left" /> and <paramref name="right" />.</summary>
    public static DuplicateScoreEvaluation Evaluate(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var tags = new List<string>(10);
        var anchorScore = ScoreAnchors(left, right, tags);
        if (anchorScore == 0)
        {
            return EmptyEvaluation();
        }

        var corroborationScore = ScoreCorroboratingAttributes(left, right, tags);
        var earnedScore = Math.Max(0, anchorScore + corroborationScore);
        if (earnedScore == 0)
        {
            return EmptyEvaluation();
        }

        var availableWeight = Math.Max(ComputeAvailableWeight(left), ComputeAvailableWeight(right));
        var confidence = ComputeConfidenceScore(earnedScore, availableWeight, MaxPossibleWeight);

        return new DuplicateScoreEvaluation(
            IsCandidateMatch: true,
            earnedScore,
            availableWeight,
            MaxPossibleWeight,
            confidence,
            string.Join('|', tags)
        );
    }

    /// <summary>Computes the completeness-damped weighted confidence percentage (0–100).</summary>
    public static decimal ComputeConfidenceScore(
        int earnedWeight,
        int availableWeight,
        int maxPossibleWeight = MaxPossibleWeight
    )
    {
        if (earnedWeight <= 0 || availableWeight <= 0 || maxPossibleWeight <= 0)
        {
            return 0m;
        }

        var matchRatio = Math.Clamp((double)earnedWeight / availableWeight, 0d, 1d);
        var completenessFactor =
            0.55d + (0.45d * Math.Clamp((double)availableWeight / maxPossibleWeight, 0d, 1d));
        var scaled = Math.Clamp(matchRatio * completenessFactor * 100d, 0d, 100d);
        return Math.Round((decimal)scaled, 2, MidpointRounding.AwayFromZero);
    }

    private static DuplicateScoreEvaluation EmptyEvaluation()
    {
        return new DuplicateScoreEvaluation(
            IsCandidateMatch: false,
            Score: 0,
            Capacity: 0,
            MaxPossibleWeight,
            ConfidenceScore: 0m,
            string.Empty
        );
    }

    private static int ScoreAnchors(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = 0;
        if (MatchesEmail(left, right))
        {
            score += EmailWeight;
            tags.Add("Email");
        }

        if (MatchesNameAnchor(left, right))
        {
            score += NameAnchorWeight;
            tags.Add("NameAnchor");
        }

        if (AreDifferentHouseholds(left, right))
        {
            score += ScoreCrossHouseholdContactAnchors(left, right, tags);
        }

        return score;
    }

    private static int ScoreCrossHouseholdContactAnchors(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = 0;
        if (SharesAny(left.CellPhoneKeys, right.CellPhoneKeys))
        {
            score += MobilePhoneWeight;
            tags.Add("CellPhone");
        }

        if (SharesAny(left.HomePhoneKeys, right.HomePhoneKeys))
        {
            score += LandlinePhoneWeight;
            tags.Add("Phone");
        }

        if (
            left.PrimaryHomeLocationId.HasValue
            && left.PrimaryHomeLocationId == right.PrimaryHomeLocationId
            && left.PrimaryHouseholdRole == right.PrimaryHouseholdRole
        )
        {
            score += HomeAddressWeight;
            tags.Add("Address");
        }

        return score;
    }

    private static int ScoreCorroboratingAttributes(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = ScoreNameCorroboration(left, right, tags);
        score += ScoreBirthDateAndBranch(left, right, tags);
        score += ScoreSuffixSignal(left.Suffix, right.Suffix, tags);
        return score;
    }

    private static int ScoreNameCorroboration(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = 0;
        if (MatchesFullFirstNameOrNickname(left, right))
        {
            score += FirstNameWeight;
            tags.Add("FirstName");
        }

        if (EqualsCI(left.LastName, right.LastName))
        {
            score += LastNameWeight;
            tags.Add("LastName");
        }

        return score;
    }

    private static int ScoreBirthDateAndBranch(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right,
        List<string> tags
    )
    {
        var score = 0;
        if (left.BirthDate is { } dob && right.BirthDate == dob)
        {
            score += BirthDateWeight;
            tags.Add("Birthdate");
        }

        if (left.PrimaryBranchId is { } branchId && right.PrimaryBranchId == branchId)
        {
            score += BranchWeight;
            tags.Add("Branch");
        }

        return score;
    }

    private static int ScoreSuffixSignal(string? leftSuffix, string? rightSuffix, List<string> tags)
    {
        if (string.IsNullOrWhiteSpace(leftSuffix) || string.IsNullOrWhiteSpace(rightSuffix))
        {
            return 0;
        }

        if (
            string.Equals(leftSuffix.Trim(), rightSuffix.Trim(), StringComparison.OrdinalIgnoreCase)
        )
        {
            tags.Add("Suffix");
            return SuffixMatchWeight;
        }

        tags.Add("SuffixConflict");
        return -SuffixConflictPenalty;
    }

    private static int ComputeAvailableWeight(DuplicateCandidateProfile profile)
    {
        var weight = 0;
        if (!string.IsNullOrWhiteSpace(profile.NormalizedEmail))
        {
            weight += EmailWeight;
        }

        if (profile.CellPhoneKeys.Count > 0)
        {
            weight += MobilePhoneWeight;
        }

        if (profile.HomePhoneKeys.Count > 0)
        {
            weight += LandlinePhoneWeight;
        }

        if (profile.PrimaryHomeLocationId.HasValue)
        {
            weight += HomeAddressWeight;
        }

        weight += ComputeDemographicWeight(profile);
        return Math.Clamp(weight, 10, MaxPossibleWeight);
    }

    private static int ComputeDemographicWeight(DuplicateCandidateProfile profile)
    {
        var weight = 0;
        if (
            !string.IsNullOrWhiteSpace(profile.FirstName)
            || !string.IsNullOrWhiteSpace(profile.NickName)
        )
        {
            weight += FirstNameWeight + NameAnchorWeight;
        }

        if (!string.IsNullOrWhiteSpace(profile.LastName))
        {
            weight += LastNameWeight;
        }

        if (profile.BirthDate.HasValue)
        {
            weight += BirthDateWeight;
        }

        if (profile.PrimaryBranchId.HasValue)
        {
            weight += BranchWeight;
        }

        if (!string.IsNullOrWhiteSpace(profile.Suffix))
        {
            weight += SuffixMatchWeight;
        }

        return weight;
    }

    private static bool MatchesEmail(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        return !string.IsNullOrWhiteSpace(left.NormalizedEmail)
            && string.Equals(left.NormalizedEmail, right.NormalizedEmail, StringComparison.Ordinal);
    }

    private static bool MatchesNameAnchor(
        DuplicateCandidateProfile left,
        DuplicateCandidateProfile right
    )
    {
        var sameLastOrPhonetic =
            EqualsCI(left.LastName, right.LastName)
            || (
                !string.IsNullOrWhiteSpace(left.LastNameMetaphone)
                && string.Equals(
                    left.LastNameMetaphone,
                    right.LastNameMetaphone,
                    StringComparison.Ordinal
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
        return !string.IsNullOrWhiteSpace(left)
            && !string.IsNullOrWhiteSpace(right)
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
