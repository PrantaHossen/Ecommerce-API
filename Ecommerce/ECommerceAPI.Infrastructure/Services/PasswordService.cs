using ECommerceAPI.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceAPI.Infrastructure.Services;

/// <summary>
/// Password hashing using PBKDF2 (RFC 2898) with:
/// - 32-byte cryptographically random salt
/// - SHA-512 HMAC
/// - 350,000 iterations (OWASP 2023 recommendation)
/// - 64-byte output hash
/// </summary>
public class PasswordService : IPasswordService
{
    private const int SaltSize = 32;       // 256 bits
    private const int HashSize = 64;       // 512 bits
    private const int Iterations = 350_000; // OWASP recommended

    public string GenerateSalt()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(saltBytes);
    }

    public string HashPassword(string plainPassword, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var passwordBytes = Encoding.UTF8.GetBytes(plainPassword);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password: passwordBytes,
            salt: saltBytes,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA512,
            outputLength: HashSize);

        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string plainPassword, string storedHash, string storedSalt)
    {
        var computedHash = HashPassword(plainPassword, storedSalt);

        // CryptographicOperations.FixedTimeEquals prevents timing attacks
        var computedBytes = Convert.FromBase64String(computedHash);
        var storedBytes = Convert.FromBase64String(storedHash);

        return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
    }
}
