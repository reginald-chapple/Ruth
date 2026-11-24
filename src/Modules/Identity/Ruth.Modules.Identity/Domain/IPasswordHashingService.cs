namespace Ruth.Modules.Identity.Domain;

/// <summary>Hashes and verifies account passwords without coupling the domain to ASP.NET Core Identity.</summary>
public interface IPasswordHashingService
{
    /// <summary>Computes a salted cryptographic password hash.</summary>
    string HashPassword(string plaintextPassword);

    /// <summary>Verifies whether <paramref name="providedPassword" /> matches <paramref name="hashedPassword" />.</summary>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
