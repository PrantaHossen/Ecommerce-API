namespace ECommerceAPI.Application.DTOs.Auth;

// ─── Requests ────────────────────────────────────────────────────────────────

public record RegisterRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);

// ─── Responses ───────────────────────────────────────────────────────────────

public record AuthResponse(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt
);

public record UserProfileResponse(
    Guid UserId,
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    bool IsEmailVerified,
    DateTime CreatedAt
);
