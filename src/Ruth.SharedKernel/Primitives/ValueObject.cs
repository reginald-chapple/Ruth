namespace Ruth.SharedKernel.Primitives;

/// <summary>
/// Base class for complex DDD value objects with structural equality over ordered components.
/// Prefer readonly record structs for simple value objects without collection fields.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Returns the ordered sequence of components that define structural equality.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        return other is not null
            && (
                ReferenceEquals(this, other)
                || (
                    GetType() == other.GetType()
                    && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents())
                )
            );
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ValueObject other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GetType());

        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    /// <summary>Determines whether two value objects are structurally equal.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    /// <summary>Determines whether two value objects are not structurally equal.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
