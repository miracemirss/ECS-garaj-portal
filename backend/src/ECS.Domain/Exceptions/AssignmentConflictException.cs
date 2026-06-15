namespace ECS.Domain.Exceptions;

/// <summary>Raised when an assignment violates the "one active link" rule.</summary>
public sealed class AssignmentConflictException : DomainException
{
    public AssignmentConflictException(string message) : base(message)
    {
    }
}
