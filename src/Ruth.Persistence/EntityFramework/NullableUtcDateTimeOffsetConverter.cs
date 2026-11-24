using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Persistence.EntityFramework;

internal sealed class NullableUtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset?, DateTimeOffset?>(
        static value => value.HasValue ? value.Value.ToUniversalTime() : null,
        static value => value.HasValue ? value.Value.ToUniversalTime() : null
    );
