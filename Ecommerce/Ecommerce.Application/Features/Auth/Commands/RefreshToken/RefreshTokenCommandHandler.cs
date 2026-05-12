using ECommerceAPI.Application.Common.Models;
using ECommerceAPI.Application.DTOs.Auth;
using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Interfaces;
using MediatR;

namespace ECommerceAPI.Application.Features.Auth.Commands.RefreshToken;

// ─── Command ─────────────────────────────────────────────────────────────────

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken
) : IRequest<Result<AuthResponse>>;

// ─── Handler ─────────────────────────────────────────────────────────────────

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Extract userId from the EXPIRED access token (still valid signature)
        var userId = _jwtService.GetUserIdFromExpiredToken(request.AccessToken);

        if (userId is null)
            return Result<AuthResponse>.Failure("Invalid access token.", 401);

        // 2. Load user
        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("User not found or inactive.", 401);

        // 3. Validate refresh token (checks value AND expiry)
        if (!user.IsRefreshTokenValid(request.RefreshToken))
            return Result<AuthResponse>.Failure("Refresh token is invalid or expired.", 401);

        // 4. Issue brand new tokens (rotation — old refresh token invalidated)
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        user.UpdateRefreshToken(newRefreshToken, refreshExpiry);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role.ToString(),
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
        ));
    }
}
