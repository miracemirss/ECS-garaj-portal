using ECS.Application.Common.Interfaces;

namespace ECS.Infrastructure.Excel;

/// <summary>
/// ClosedXML implementation of <see cref="IExcelService"/>. Column mapping is
/// implemented in the reporting prompt.
/// </summary>
public sealed class ClosedXmlExportService : IExcelService
{
    public byte[] Export<T>(IEnumerable<T> rows, string sheetName = "Sheet1")
        => throw new NotImplementedException(
            "Excel export is implemented in the reporting prompt.");
}
