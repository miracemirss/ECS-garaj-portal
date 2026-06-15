using System.Security.Cryptography;
using ECS.Application.Common.Interfaces;

namespace ECS.Infrastructure.Identity;

/// <summary>PBKDF2 (SHA-256) password hashing. Format: "{iterations}.{salt}.{hash}" (base64).</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string hash, string password)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var key = Convert.FromBase64String(parts[2]);
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, key.Length);
        return CryptographicOperations.FixedTimeEquals(computed, key);
    }
}
