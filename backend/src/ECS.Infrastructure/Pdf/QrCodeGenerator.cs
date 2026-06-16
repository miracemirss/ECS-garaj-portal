using ECS.Application.Common.Interfaces;
using QRCoder;

namespace ECS.Infrastructure.Pdf;

/// <summary>
/// <see cref="IQrCodeGenerator"/> backed by QRCoder's <see cref="PngByteQRCode"/>,
/// which renders without System.Drawing and therefore works in headless Linux
/// containers. Failures return an empty array so a report never fails to render
/// just because the QR could not be produced.
/// </summary>
public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 6)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return [];
        }

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule);
        }
        catch
        {
            return [];
        }
    }
}
