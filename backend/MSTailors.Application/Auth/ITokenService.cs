using MSTailors.Domain.Entities;

namespace MSTailors.Application.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(Customer customer);

    /// <summary>Returns the raw token to hand to the client and the hash to persist.</summary>
    (string Raw, string Hash, DateTime ExpiresAtUtc) CreateRefreshToken();

    /// <summary>Hashes a presented token so it can be compared against stored hashes.</summary>
    string HashToken(string rawToken);
}

public interface IPasswordHashingService
{
    string Hash(Customer customer, string password);

    /// <summary>True when the password matches. Rehash is signalled separately.</summary>
    bool Verify(Customer customer, string hash, string password, out bool needsRehash);
}
