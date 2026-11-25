namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Built-in linguistic reference seed data for first-name gender and nickname expansion (zero sample people).</summary>
internal static class MetaLookupReferenceData
{
    private static readonly (string First, int Male, int Female)[] _genderRows =
    [
        ("Robert", 9920, 80),
        ("William", 9940, 60),
        ("James", 9910, 90),
        ("John", 9930, 70),
        ("Michael", 9900, 100),
        ("David", 9940, 60),
        ("Richard", 9950, 50),
        ("Christopher", 9920, 80),
        ("Daniel", 9910, 90),
        ("Matthew", 9940, 60),
        ("Elizabeth", 30, 9970),
        ("Katherine", 20, 9980),
        ("Catherine", 20, 9980),
        ("Margaret", 20, 9980),
        ("Jennifer", 30, 9970),
        ("Rebecca", 20, 9980),
        ("Sarah", 30, 9970),
        ("Jessica", 30, 9970),
        ("Taylor", 4800, 5200),
        ("Jordan", 5400, 4600),
        ("Alex", 5800, 4200),
    ];

    private static readonly (string First, string Nick, string Gender)[] _nickRows =
    [
        ("Robert", "Bob", "M"),
        ("Robert", "Rob", "M"),
        ("Robert", "Bobby", "M"),
        ("William", "Bill", "M"),
        ("William", "Will", "M"),
        ("William", "Liam", "M"),
        ("James", "Jim", "M"),
        ("James", "Jimmy", "M"),
        ("John", "Jack", "M"),
        ("John", "Johnny", "M"),
        ("Michael", "Mike", "M"),
        ("David", "Dave", "M"),
        ("Richard", "Rick", "M"),
        ("Richard", "Rich", "M"),
        ("Christopher", "Chris", "M"),
        ("Daniel", "Dan", "M"),
        ("Daniel", "Danny", "M"),
        ("Matthew", "Matt", "M"),
        ("Elizabeth", "Liz", "F"),
        ("Elizabeth", "Beth", "F"),
        ("Elizabeth", "Betty", "F"),
        ("Katherine", "Kate", "F"),
        ("Katherine", "Katie", "F"),
        ("Katherine", "Kathy", "F"),
        ("Catherine", "Cathy", "F"),
        ("Catherine", "Kate", "F"),
        ("Margaret", "Maggie", "F"),
        ("Margaret", "Meg", "F"),
        ("Jennifer", "Jen", "F"),
        ("Jennifer", "Jenny", "F"),
        ("Rebecca", "Becky", "F"),
        ("Rebecca", "Becca", "F"),
        ("Alexander", "Alex", "M"),
        ("Alexandra", "Alex", "F"),
        ("Stephen", "Steve", "M"),
        ("Steven", "Steve", "M"),
    ];

    public static IReadOnlyList<MetaFirstNameGenderLookup> BuildGenderRows()
    {
        return
        [
            .. _genderRows.Select(
                static (row, idx) =>
                    new MetaFirstNameGenderLookup(
                        CreateDeterministicGuid(1, idx + 1),
                        row.First,
                        row.Male,
                        row.Female
                    )
            ),
        ];
    }

    public static IReadOnlyList<MetaNickNameLookup> BuildNickNameRows()
    {
        return
        [
            .. _nickRows.Select(
                static (row, idx) =>
                    new MetaNickNameLookup(
                        CreateDeterministicGuid(2, idx + 1),
                        row.First,
                        row.Nick,
                        row.Gender
                    )
            ),
        ];
    }

    private static Guid CreateDeterministicGuid(byte group, int ordinal)
    {
        return new Guid(
            0x2200f000,
            group,
            0x4b51,
            0x91,
            0x02,
            0xaa,
            0x00,
            0x00,
            0x00,
            (byte)(ordinal >> 8),
            (byte)(ordinal & 0xFF)
        );
    }
}
