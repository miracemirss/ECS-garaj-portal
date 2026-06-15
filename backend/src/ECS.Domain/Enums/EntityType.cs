namespace ECS.Domain.Enums;

/// <summary>
/// Identifies whether a maintenance/operation target is a vehicle or a trailer.
/// A work order targets exactly one of these (Vehicle XOR Trailer).
/// </summary>
public enum EntityType
{
    Vehicle = 1,
    Trailer = 2
}
