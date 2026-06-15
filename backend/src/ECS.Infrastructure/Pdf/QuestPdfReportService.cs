using ECS.Application.Common.Interfaces;

namespace ECS.Infrastructure.Pdf;

/// <summary>
/// QuestPDF implementation of <see cref="IPdfService"/>. Document composition
/// (e.g. the maintenance work-order report) is implemented in the reporting prompt.
/// </summary>
public sealed class QuestPdfReportService : IPdfService
{
    public byte[] Render(IPdfDocumentModel model)
        => throw new NotImplementedException(
            "PDF rendering is implemented in the maintenance reporting prompt.");
}
