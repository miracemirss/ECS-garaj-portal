using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Reports.Models;
using QuestPDF.Fluent;

namespace ECS.Infrastructure.Pdf;

/// <summary>
/// QuestPDF implementation of <see cref="IPdfService"/>. Resolves the concrete
/// document for a given model and renders it to a PDF byte array. The QuestPDF
/// Community license is configured once at startup in Infrastructure DI.
/// </summary>
public sealed class QuestPdfReportService : IPdfService
{
    private readonly ReportBrandAssets _assets;
    private readonly IQrCodeGenerator _qr;

    public QuestPdfReportService(ReportBrandAssets assets, IQrCodeGenerator qr)
    {
        _assets = assets;
        _qr = qr;
    }

    public byte[] Render(IPdfDocumentModel model) => model switch
    {
        MaintenanceReportModel maintenance => RenderMaintenance(maintenance),
        _ => throw new NotSupportedException($"No PDF renderer is registered for '{model.GetType().Name}'.")
    };

    private byte[] RenderMaintenance(MaintenanceReportModel model)
    {
        var qr = _qr.GeneratePng(model.VerificationPayload);
        var document = new MaintenanceReportDocument(model, _assets.Logo, qr);
        return document.GeneratePdf();
    }
}
