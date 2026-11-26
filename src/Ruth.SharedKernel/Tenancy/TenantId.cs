using System.Security.Cryptography;
using Ruth.SharedKernel.Primitives;

namespace Ruth.SharedKernel.Tenancy;

/// <summary>Strongly-typed identifier representing an isolated organization tenant.</summary>
public readonly record struct TenantId(Guid Value) : IStronglyTypedId<TenantId>
{
    /// <summary>Represents an unset / unauthenticated tenant identifier.</summary>
    public static TenantId Empty => new(Guid.Empty);

    /// <summary>Gets whether this tenant identifier is empty.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>Creates a new time-ordered UUIDv7 <see cref="TenantId" />.</summary>
    public static TenantId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing <see cref="Guid" /> as a <see cref="TenantId" />.</summary>
    public static TenantId From(Guid value)
    {
        return new(value);
    }

    /// <summary>
    /// Derives a deterministic, collision-free per-tenant <see cref="Guid" /> from <paramref name="seedGuid" />
    /// so built-in per-tenant system records never collide on primary key across tenants.
    /// </summary>
    public Guid DeriveDeterministicGuid(Guid seedGuid)
    {
        if (IsEmpty)
        {
            return seedGuid;
        }

        Span<byte> input = stackalloc byte[32];
        _ = Value.TryWriteBytes(input[..16]);
        _ = seedGuid.TryWriteBytes(input[16..]);

        Span<byte> hash = stackalloc byte[32];
        _ = SHA256.HashData(input, hash);

        Span<byte> guidBytes = stackalloc byte[16];
        hash[..16].CopyTo(guidBytes);
        // Set RFC 9562 version 8 (custom deterministic) and variant bits.
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x80);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
