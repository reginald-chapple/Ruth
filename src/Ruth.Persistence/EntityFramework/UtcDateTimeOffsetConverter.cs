using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ruth.Persistence.EntityFramework;

internal sealed class UtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset, DateTimeOffset>(
        static value => value.ToUniversalTime(),
        static value => value.ToUniversalTime()
    );
