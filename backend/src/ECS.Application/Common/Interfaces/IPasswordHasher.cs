namespace ECS.Application.Common.Interfaces;

/// <summary>Hashes and verifies user passwords. Implemented in Infrastructure (PBKDF2).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
