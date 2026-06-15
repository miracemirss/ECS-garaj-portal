namespace ECS.Domain.Exceptions;

/// <summary>Raised when an operation is invalid for the work order's current status.</summary>
public sealed class InvalidWorkOrderStateException : DomainException
{
    public InvalidWorkOrderStateException(string message) : base(message)
    {
    }
}
