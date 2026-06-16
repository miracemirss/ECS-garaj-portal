namespace ECS.Application.Common.Interfaces;

/// <summary>
/// Binary file storage port. Generated artifacts (PDF reports, Excel exports) are
/// written here and their relative path is persisted on the owning metadata row
/// (e.g. <c>report_files.file_path</c>) so the file can be streamed again later.
/// The Infrastructure implementation is filesystem-backed by default; the same
/// port can be implemented over S3/Azure Blob without touching the Application
/// layer.
/// </summary>
public interface IFileStorage
{
    /// <summary>Writes <paramref name="content"/> and returns the normalized relative path actually stored.</summary>
    Task<string> SaveAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default);

    /// <summary>Reads a previously stored file, or <c>null</c> when it is missing.</summary>
    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
