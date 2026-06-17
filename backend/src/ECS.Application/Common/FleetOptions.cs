namespace ECS.Application.Common;

public static class FleetOptions
{
    public static readonly string[] PartUnits =
    [
        "Adet",
        "Litre",
        "Kilogram",
        "Metre",
        "Takim",
        "Kutu",
        "Paket",
        "Cift"
    ];

    public static readonly string[] TrailerTypes =
    [
        "Tenteli Dorse",
        "Frigorifik Dorse",
        "Lowbed",
        "Platform Dorse",
        "Konteyner Tasiyici",
        "Silobas",
        "Tanker",
        "Kapali Kasa",
        "Sal Dorse",
        "Mega Dorse"
    ];

    public static bool IsValidPartUnit(string? unit)
        => !string.IsNullOrWhiteSpace(unit)
           && PartUnits.Any(x => string.Equals(x, unit.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsValidTrailerType(string? trailerType)
        => string.IsNullOrWhiteSpace(trailerType)
           || TrailerTypes.Any(x => string.Equals(x, trailerType.Trim(), StringComparison.OrdinalIgnoreCase));
}
