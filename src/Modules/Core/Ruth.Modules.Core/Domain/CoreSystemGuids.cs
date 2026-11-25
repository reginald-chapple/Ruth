namespace Ruth.Modules.Core.Domain;

/// <summary>Deterministic SystemGuid constants for Core entities and foundational FieldTypes.</summary>
public static class CoreSystemGuids
{
    /// <summary>EntityType SystemGuid for EntityTypeDescriptor.</summary>
    public static readonly Guid EntityTypeDescriptor = new(
        0x10a77701,
        0x4c10,
        0x4b51,
        0x91,
        0x01,
        0xaa,
        0x00,
        0x00,
        0x00,
        0x00,
        0x01
    );

    /// <summary>EntityType SystemGuid for DefinedType.</summary>
    public static readonly Guid DefinedType = new(
        0x6d902c01,
        0x8599,
        0x4b22,
        0xa7,
        0x11,
        0x5b,
        0x1d,
        0x90,
        0x02,
        0x00,
        0x02
    );

    /// <summary>EntityType SystemGuid for DefinedValue.</summary>
    public static readonly Guid DefinedValue = new(
        0x6d902c01,
        0x8599,
        0x4b22,
        0xa7,
        0x11,
        0x5b,
        0x1d,
        0x90,
        0x02,
        0x00,
        0x03
    );

    /// <summary>EntityType SystemGuid for Category.</summary>
    public static readonly Guid Category = new(
        0x1d68154e,
        0xec76,
        0x44c8,
        0x98,
        0x13,
        0x77,
        0x36,
        0xb2,
        0x7a,
        0xec,
        0xf9
    );

    /// <summary>EntityType SystemGuid for AttributeDefinition.</summary>
    public static readonly Guid AttributeDefinition = new(
        0x5997c8d3,
        0x8840,
        0x4591,
        0x99,
        0xa5,
        0x55,
        0x29,
        0x19,
        0xf9,
        0x0f,
        0xce
    );

    /// <summary>EntityType SystemGuid for AttributeMatrixTemplate.</summary>
    public static readonly Guid AttributeMatrixTemplate = new(
        0x20b90f35,
        0x70c1,
        0x4adc,
        0xa9,
        0x08,
        0xa4,
        0x25,
        0x4c,
        0x15,
        0x37,
        0x3d
    );

    /// <summary>EntityType SystemGuid for Branch.</summary>
    public static readonly Guid Branch = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x05
    );

    /// <summary>EntityType SystemGuid for Location.</summary>
    public static readonly Guid Location = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x06
    );

    /// <summary>EntityType SystemGuid for Device.</summary>
    public static readonly Guid Device = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x07
    );

    /// <summary>EntityType SystemGuid for Schedule.</summary>
    public static readonly Guid Schedule = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x08
    );

    /// <summary>EntityType SystemGuid for BinaryFileType.</summary>
    public static readonly Guid BinaryFileType = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x09
    );

    /// <summary>EntityType SystemGuid for BinaryFile.</summary>
    public static readonly Guid BinaryFile = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x0a
    );

    /// <summary>EntityType SystemGuid for DocumentType.</summary>
    public static readonly Guid DocumentType = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x0b
    );

    /// <summary>EntityType SystemGuid for Document.</summary>
    public static readonly Guid Document = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x0c
    );

    /// <summary>EntityType SystemGuid for SignatureDocumentTemplate.</summary>
    public static readonly Guid SignatureDocumentTemplate = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x0d
    );

    /// <summary>EntityType SystemGuid for SignatureDocument.</summary>
    public static readonly Guid SignatureDocument = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x0e
    );

    /// <summary>EntityType SystemGuid for HistoryLogEntry.</summary>
    public static readonly Guid HistoryLogEntry = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x0f
    );

    /// <summary>EntityType SystemGuid for NoteType.</summary>
    public static readonly Guid NoteType = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x10
    );

    /// <summary>EntityType SystemGuid for Note.</summary>
    public static readonly Guid Note = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x11
    );

    /// <summary>EntityType SystemGuid for Tag.</summary>
    public static readonly Guid Tag = new(
        0x00096bed,
        0x9587,
        0x415e,
        0x8b,
        0x73,
        0x62,
        0x6a,
        0x00,
        0x00,
        0x01,
        0x12
    );
}
