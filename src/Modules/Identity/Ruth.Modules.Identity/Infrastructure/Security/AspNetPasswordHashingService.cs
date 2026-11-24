using Microsoft.AspNetCore.Identity;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed class AspNetPasswordHashingService : IPasswordHashingService
{
    private static readonly PasswordHashUser _hashUser = new();
    private readonly PasswordHasher<PasswordHashUser> _hasher = new();

    public string HashPassword(string plaintextPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintextPassword);
        return _hasher.HashPassword(_hashUser, plaintextPassword);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(_hashUser, hashedPassword, providedPassword);
        return result
            is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private sealed class PasswordHashUser;
}
