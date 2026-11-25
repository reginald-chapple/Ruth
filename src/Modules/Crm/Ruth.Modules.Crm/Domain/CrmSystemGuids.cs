namespace Ruth.Modules.Crm.Domain;

/// <summary>Deterministic SystemGuid constants for CRM bounded-context entities.</summary>
public static class CrmSystemGuids
{
    /// <summary>EntityType SystemGuid for Person.</summary>
    public static readonly Guid Person = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x01
    );

    /// <summary>EntityType SystemGuid for Household.</summary>
    public static readonly Guid Household = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x02
    );

    /// <summary>EntityType SystemGuid for KnownRelationship.</summary>
    public static readonly Guid KnownRelationship = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x03
    );

    /// <summary>EntityType SystemGuid for SignalType.</summary>
    public static readonly Guid SignalType = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x04
    );

    /// <summary>EntityType SystemGuid for PersonSignal.</summary>
    public static readonly Guid PersonSignal = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x05
    );

    /// <summary>EntityType SystemGuid for BadgeDefinition.</summary>
    public static readonly Guid BadgeDefinition = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x06
    );

    /// <summary>EntityType SystemGuid for AssessmentType.</summary>
    public static readonly Guid AssessmentType = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x07
    );

    /// <summary>EntityType SystemGuid for Assessment.</summary>
    public static readonly Guid Assessment = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x08
    );

    /// <summary>EntityType SystemGuid for BackgroundCheck.</summary>
    public static readonly Guid BackgroundCheck = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x09
    );

    /// <summary>EntityType SystemGuid for PersonDuplicate.</summary>
    public static readonly Guid PersonDuplicate = new(
        0x2200c001,
        0x4c10,
        0x4b51,
        0x91,
        0x02,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x02,
        0x0a
    );
}
