using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// EF Core value converter mapping <see cref="IStronglyTypedId{TSelf}" /> structs to PostgreSQL UUID columns.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier struct.</typeparam>
public sealed class StronglyTypedIdValueConverter<TId>()
    : ValueConverter<TId, Guid>(
        static id => ConvertToGuid(id),
        static guid => ConvertFromGuid(guid)
    )
    where TId : struct, IStronglyTypedId<TId>
{
    private static Guid ConvertToGuid(TId id)
    {
        return id.Value;
    }

    private static TId ConvertFromGuid(Guid guid)
    {
        return TId.From(guid);
    }
}
