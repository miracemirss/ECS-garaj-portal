namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the system clock so time-dependent rules (maintenance-due
/// checks, assignment start/end) stay testable.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
