using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Branch" />.</summary>
public readonly record struct BranchId(Guid Value) : IStronglyTypedId<BranchId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static BranchId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 branch identifier.</summary>
    public static BranchId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static BranchId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
