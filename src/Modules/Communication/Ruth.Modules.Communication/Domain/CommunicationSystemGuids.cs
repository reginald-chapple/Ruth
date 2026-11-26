namespace Ruth.Modules.Communication.Domain;

/// <summary>Deterministic SystemGuid constants for Communication module entity types and built-in records.</summary>
public static class CommunicationSystemGuids
{
    /// <summary>EntityType SystemGuid for TemplateSnippet.</summary>
    public static readonly Guid TemplateSnippet = new(
        0x7574a473,
        0x3326,
        0x4973,
        0x8d,
        0xf6,
        0xc7,
        0xbf,
        0x5f,
        0x64,
        0xeb,
        0x36
    );

    /// <summary>EntityType SystemGuid for SystemCommunication.</summary>
    public static readonly Guid SystemCommunication = new(
        0x6a9c3101,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x01
    );

    /// <summary>EntityType SystemGuid for SystemPhoneNumber.</summary>
    public static readonly Guid SystemPhoneNumber = new(
        0x6a9c3102,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x02
    );

    /// <summary>EntityType SystemGuid for PersonalDevice.</summary>
    public static readonly Guid PersonalDevice = new(
        0x6a9c3103,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x03
    );

    /// <summary>Seeded system snippet: organization footer.</summary>
    public static readonly Guid OrgFooterSnippet = new(
        0x6a9c3201,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x20,
        0x01
    );

    /// <summary>Seeded system snippet: support contact block.</summary>
    public static readonly Guid SupportContactSnippet = new(
        0x6a9c3202,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x20,
        0x02
    );

    /// <summary>Seeded SystemCommunication: Organization Onboarded.</summary>
    public static readonly Guid SysCommOrganizationOnboarded = new(
        0x6a9c3301,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x30,
        0x01
    );

    /// <summary>Seeded SystemCommunication: Password Reset.</summary>
    public static readonly Guid SysCommPasswordReset = new(
        0x6a9c3302,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x30,
        0x02
    );

    /// <summary>Seeded SystemCommunication: Account Confirmation.</summary>
    public static readonly Guid SysCommAccountConfirmation = new(
        0x6a9c3303,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x30,
        0x03
    );

    /// <summary>Seeded SystemCommunication: Group Attendance Recorded.</summary>
    public static readonly Guid SysCommGroupAttendanceRecorded = new(
        0x6a9c3304,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x30,
        0x04
    );

    /// <summary>Seeded SystemCommunication: Finance Payment or Gift Receipt.</summary>
    public static readonly Guid SysCommFinanceReceipt = new(
        0x6a9c3305,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x30,
        0x05
    );

    /// <summary>Seeded SystemCommunication: Workflow Task Assigned.</summary>
    public static readonly Guid SysCommWorkflowTaskAssigned = new(
        0x6a9c3306,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x30,
        0x06
    );

    /// <summary>EntityType SystemGuid for CommunicationTemplate.</summary>
    public static readonly Guid CommunicationTemplate = new(
        0x6a9c3104,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x04
    );

    /// <summary>EntityType SystemGuid for Communication.</summary>
    public static readonly Guid Communication = new(
        0x6a9c3105,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x05
    );

    /// <summary>EntityType SystemGuid for SmsPipeline.</summary>
    public static readonly Guid SmsPipeline = new(
        0x6a9c3106,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x06
    );

    /// <summary>EntityType SystemGuid for CommunicationResponse.</summary>
    public static readonly Guid CommunicationResponse = new(
        0x6a9c3107,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x07
    );

    /// <summary>EntityType SystemGuid for CommunicationFlow.</summary>
    public static readonly Guid CommunicationFlow = new(
        0x6a9c3108,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x10,
        0x08
    );

    /// <summary>Seeded bulk CommunicationTemplate: Member &amp; Community Digest.</summary>
    public static readonly Guid SeededMemberDigestTemplate = new(
        0x6a9c3401,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x40,
        0x01
    );

    /// <summary>Seeded system SmsPipeline: Standard Member SMS Responder.</summary>
    public static readonly Guid SeededMemberSmsPipeline = new(
        0x6a9c3402,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x40,
        0x02
    );

    /// <summary>Seeded system CommunicationFlow: New Member Onboarding Drip.</summary>
    public static readonly Guid SeededMemberOnboardingFlow = new(
        0x6a9c3403,
        0x72d1,
        0x489a,
        0x91,
        0x22,
        0x11,
        0x4b,
        0x88,
        0x90,
        0x40,
        0x03
    );
}
