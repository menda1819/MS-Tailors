using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MSTailors.Application.Auth;
using MSTailors.Domain.Entities;

namespace MSTailors.Infrastructure.Auth;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(Customer customer)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id),
            new(JwtRegisteredClaimNames.Email, customer.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, customer.Id),
            // The role claim is what the [Authorize(Roles = "Admin")] policy
            // reads. It comes from the database, never from the request.
            new(ClaimTypes.Role, customer.Role.ToString()),
            new("name", customer.FullName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string Raw, string Hash, DateTime ExpiresAtUtc) CreateRefreshToken()
    {
        // 256 bits from a CSPRNG. Guessing one is not feasible, so the token
        // itself is the credential and needs no additional entropy.
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);

        return (raw, HashToken(raw), DateTime.UtcNow.AddDays(_settings.RefreshTokenDays));
    }

    public string HashToken(string rawToken)
    {
        // Only the hash is persisted, so a database dump yields no usable
        // tokens. SHA-256 is right here: the input is already high-entropy, so
        // a slow password hash would buy nothing but latency.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}

public sealed class PasswordHashingService : IPasswordHashingService
{
    private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<Customer> _hasher;

    public PasswordHashingService(Microsoft.AspNetCore.Identity.IPasswordHasher<Customer> hasher) =>
        _hasher = hasher;

    public string Hash(Customer customer, string password) => _hasher.HashPassword(customer, password);

    public bool Verify(Customer customer, string hash, string password, out bool needsRehash)
    {
        needsRehash = false;

        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(customer, hash, password);

        needsRehash = result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded;

        return result != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
    }
}
