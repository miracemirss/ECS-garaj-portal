namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Excel export port. Implemented in Infrastructure with ClosedXML.
/// </summary>
public interface IExcelService
{
    byte[] Export<T>(IEnumerable<T> rows, string sheetName = "Sheet1");
}
