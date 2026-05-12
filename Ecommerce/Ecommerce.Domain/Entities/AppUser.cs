using ECommerceAPI.Domain.Common;
using ECommerceAPI.Domain.Enums;

namespace ECommerceAPI.Domain.Entities;

public class AppUser : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;

    // Password stored as PBKDF2 hash — never plain text
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;

    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsActive { get; private set; } = true;
    public bool IsEmailVerified { get; private set; } = false;

    // Refresh token fields
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    // EF Core needs this
    private AppUser() { }

    // Factory method — only way to create a user
    public static AppUser Create(
        string fullName,
        string email,
        string phoneNumber,
        string passwordHash,
        string passwordSalt,
        UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.");

        return new AppUser
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PhoneNumber = phoneNumber.Trim(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = role
        };
    }

    public void UpdateRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
        MarkAsUpdated();
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        MarkAsUpdated();
    }

    public bool IsRefreshTokenValid(string token)
        => RefreshToken == token
           && RefreshTokenExpiresAt.HasValue
           && RefreshTokenExpiresAt.Value > DateTime.UtcNow;

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void UpdateProfile(string fullName, string phoneNumber)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
            FullName = fullName.Trim();
        if (!string.IsNullOrWhiteSpace(phoneNumber))
            PhoneNumber = phoneNumber.Trim();
        MarkAsUpdated();
    }
}
