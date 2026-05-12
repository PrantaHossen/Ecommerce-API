using ECommerceAPI.Application.Common.Models;
using ECommerceAPI.Application.DTOs.Auth;
using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Interfaces;
using MediatR;

namespace ECommerceAPI.Application.Features.Auth.Commands.Register;

// ─── Command ─────────────────────────────────────────────────────────────────

public record RegisterCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword
) : IRequest<Result<AuthResponse>>;

// ─── Handler ─────────────────────────────────────────────────────────────────

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check passwords match
        if (request.Password != request.ConfirmPassword)
            return Result<AuthResponse>.Failure("Passwords do not match.");

        // 2. Check email already exists
        var emailExists = await _userRepository.EmailExistsAsync(
            request.Email, cancellationToken);

        if (emailExists)
            return Result<AuthResponse>.Failure("Email is already registered.");

        // 3. Generate salt and hash password (PBKDF2)
        var salt = _passwordService.GenerateSalt();
        var hash = _passwordService.HashPassword(request.Password, salt);

        // 4. Create domain entity via factory method (enforces rules)
        var user = AppUser.Create(
            fullName: request.FullName,
            email: request.Email,
            phoneNumber: request.PhoneNumber,
            passwordHash: hash,
            passwordSalt: salt
        );

        // 5. Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        user.UpdateRefreshToken(refreshToken, refreshExpiry);

        // 6. Persist
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // 7. Return response
        return Result<AuthResponse>.Success(new AuthResponse(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role.ToString(),
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15)
        ), 201);
    }
}
