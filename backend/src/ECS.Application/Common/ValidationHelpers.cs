using FluentValidation.Results;

namespace ECS.Application.Common;

public static class ValidationHelpers
{
    /// <summary>Flattens FluentValidation failures into a single message for an Error.Validation.</summary>
    public static string ToMessage(this ValidationResult result)
        => string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
}
