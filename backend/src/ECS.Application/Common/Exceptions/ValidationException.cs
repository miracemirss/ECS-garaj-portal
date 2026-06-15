namespace ECS.Application.Common.Exceptions;

/// <summary>
/// Aggregates FluentValidation failures. Mapped to HTTP 400 with a field-keyed
/// error dictionary by the API's ExceptionHandlingMiddleware.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
        => Errors = new Dictionary<string, string[]>();

    public ValidationException(IReadOnlyDictionary<string, string[]> errors) : this()
        => Errors = errors;

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
