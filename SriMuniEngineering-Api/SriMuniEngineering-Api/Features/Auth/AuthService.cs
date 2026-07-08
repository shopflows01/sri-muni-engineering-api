using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.Auth.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Security;

namespace SriMuniEngineering_Api.Features.Auth;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly TokenBlacklistService _blacklistService;

    public AuthService(AppDbContext context, JwtTokenGenerator tokenGenerator, TokenBlacklistService blacklistService)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
        _blacklistService = blacklistService;
    }

    public async Task<SignupResponse> SignupAsync(SignupRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

        if (existingUser is not null)
        {
            if (existingUser.Username == request.Username)
                throw new InvalidOperationException("Username is already taken.");
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new SignupResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var (token, expiresAt) = _tokenGenerator.GenerateToken(user);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }

    public async Task ResetCredentialsAsync(ResetCredentialsRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            throw new InvalidOperationException("No account found with that email address.");

        if (string.IsNullOrWhiteSpace(request.NewUsername) && string.IsNullOrWhiteSpace(request.NewPassword))
            throw new InvalidOperationException("Please provide either a new username or a new password.");

        if (!string.IsNullOrWhiteSpace(request.NewUsername))
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.NewUsername);
            if (existingUser is not null && existingUser.Id != user.Id)
                throw new InvalidOperationException("Username is already taken.");
                
            user.Username = request.NewUsername;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        await _context.SaveChangesAsync();
    }

    public void Logout(string jti, DateTime tokenExpiry)
    {
        _blacklistService.BlacklistToken(jti, tokenExpiry);
    }
}
