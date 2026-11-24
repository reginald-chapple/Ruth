using Ruth.Modules.Identity.Infrastructure.Security;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Infrastructure;

public sealed class PasswordHasherAndStateReaderTests
{
    [Fact]
    public void AspNetPasswordHashingService_HashesSaltedAndVerifies()
    {
        var hasher = new AspNetPasswordHashingService();
        const string plain = "StrongPass#2026!";

        var hash1 = hasher.HashPassword(plain);
        var hash2 = hasher.HashPassword(plain);

        Assert.NotEqual(plain, hash1, StringComparer.Ordinal);
        Assert.NotEqual(hash1, hash2, StringComparer.Ordinal);
        Assert.True(hasher.VerifyPassword(hash1, plain));
        Assert.True(hasher.VerifyPassword(hash2, plain));
        Assert.False(hasher.VerifyPassword(hash1, "WrongPass#2026!"));
        Assert.False(hasher.VerifyPassword("", plain));
        Assert.False(hasher.VerifyPassword(hash1, ""));
        _ = Assert.Throws<ArgumentException>(() => hasher.HashPassword(" "));
    }
}
