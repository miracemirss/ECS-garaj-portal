namespace ECS.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist. Mapped to HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
