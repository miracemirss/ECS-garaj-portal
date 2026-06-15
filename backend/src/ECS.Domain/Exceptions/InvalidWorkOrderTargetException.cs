namespace ECS.Domain.Exceptions;

/// <summary>Raised when a work order's target (vehicle/trailer) is inconsistent.</summary>
public sealed class InvalidWorkOrderTargetException : DomainException
{
    public InvalidWorkOrderTargetException(string message) : base(message)
    {
    }
}
