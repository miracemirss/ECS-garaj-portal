namespace ECS.Application.Common.Interfaces;

/// <summary>
/// PDF generation port. Implemented in Infrastructure with QuestPDF. Concrete
/// document models (e.g. the maintenance report) implement IPdfDocumentModel and
/// are added in later prompts.
/// </summary>
public interface IPdfService
{
    byte[] Render(IPdfDocumentModel model);
}

/// <summary>Marker for a renderable PDF document model.</summary>
public interface IPdfDocumentModel
{
}
