namespace ECS.Domain.Exceptions;

/// <summary>
/// Thrown when a business invariant is violated (e.g. insufficient stock, a
/// vehicle already has an active trailer). Translated to a 409/400 response by
/// the API's ExceptionHandlingMiddleware.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
