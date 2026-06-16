namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Generates a QR code PNG for a verification payload printed on reports. Kept as a
/// port so the Application/document layer does not depend on a concrete QR library.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>Renders <paramref name="payload"/> to a PNG byte array. Returns an empty array if generation fails.</summary>
    byte[] GeneratePng(string payload, int pixelsPerModule = 6);
}
