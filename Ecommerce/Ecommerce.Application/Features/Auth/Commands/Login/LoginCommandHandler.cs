using ECommerceAPI.Application.Common.Models;
using ECommerceAPI.Application.DTOs.Auth;
using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Interfaces;
using MediatR;

namespace ECommerceAPI.Application.Features.Auth.Commands.Login;

// ─── Command ─────────────────────────────────────────────────────────────────

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthResponse>>;

// ─── Handler ─────────────────────────────────────────────────────────────────

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find user by email
        var user = await _userRepository.GetByEmailAsync(
            request.Email, cancellationToken);

        // 2. Generic message — never reveal if email exists or not
        if (user is null)
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);

        // 3. Check account is active
        if (!user.IsActive)
            return Result<AuthResponse>.Failure("Your account has been deactivated.", 403);

        // 4. Verify password using stored salt
        var isValid = _passwordService.VerifyPassword(
            request.Password,
            user.PasswordHash,
            user.PasswordSalt);

        if (!isValid)
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);

        // 5. Generate new tokens on every login
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        user.UpdateRefreshToken(refreshToken, refreshExpiry);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role.ToString(),
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
        ));
    }
}
