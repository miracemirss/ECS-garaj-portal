namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the caller to the Application layer without exposing
/// HttpContext. Implemented in the API layer over IHttpContextAccessor.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
}
