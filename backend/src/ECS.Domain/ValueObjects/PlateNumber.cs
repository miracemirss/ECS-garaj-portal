using ECS.Domain.Exceptions;

namespace ECS.Domain.ValueObjects;

/// <summary>
/// A normalized, validated licence plate. Entity factories call
/// <see cref="Normalize"/> so the stored plate string is always canonical
/// (trimmed, no spaces, upper-cased) and uniqueness comparisons are reliable.
/// </summary>
public sealed class PlateNumber : ValueObject
{
    public string Value { get; }

    private PlateNumber(string value) => Value = value;

    public static PlateNumber Create(string raw) => new(Normalize(raw));

    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainException("Plate number is required.");
        }

        var normalized = raw.Trim().Replace(" ", string.Empty).ToUpperInvariant();
        if (normalized.Length is < 4 or > 16)
        {
            throw new DomainException("Plate number length is invalid.");
        }

        return normalized;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
