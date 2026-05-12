namespace ECommerceAPI.Application.Interfaces;

public interface IPasswordService
{
    /// <summary>Generates a cryptographically random salt.</summary>
    string GenerateSalt();

    /// <summary>Hashes a plain-text password using PBKDF2 + the given salt.</summary>
    string HashPassword(string plainPassword, string salt);

    /// <summary>Verifies a plain-text password against the stored hash and salt.</summary>
    bool VerifyPassword(string plainPassword, string storedHash, string storedSalt);
}
