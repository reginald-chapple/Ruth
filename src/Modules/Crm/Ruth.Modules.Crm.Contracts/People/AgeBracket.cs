namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Standard demographic age brackets for reporting and segmentation.</summary>
public enum AgeBracket
{
    /// <summary>Age unknown.</summary>
    Unknown = 0,

    /// <summary>Ages 0 through 5.</summary>
    ZeroToFive = 1,

    /// <summary>Ages 6 through 12.</summary>
    SixToTwelve = 2,

    /// <summary>Ages 13 through 17.</summary>
    ThirteenToSeventeen = 3,

    /// <summary>Ages 18 through 24.</summary>
    EighteenToTwentyFour = 4,

    /// <summary>Ages 25 through 34.</summary>
    TwentyFiveToThirtyFour = 5,

    /// <summary>Ages 35 through 44.</summary>
    ThirtyFiveToFortyFour = 6,

    /// <summary>Ages 45 through 54.</summary>
    FortyFiveToFiftyFour = 7,

    /// <summary>Ages 55 through 64.</summary>
    FiftyFiveToSixtyFour = 8,

    /// <summary>Ages 65 and older.</summary>
    SixtyFiveOrOlder = 9,
}
