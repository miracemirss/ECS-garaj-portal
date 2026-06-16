using ECS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ECS.Infrastructure.Storage;

/// <summary>
/// Filesystem implementation of <see cref="IFileStorage"/>. Files are written under
/// a configurable root; the returned relative path is what gets persisted on the
/// metadata row. Path traversal outside the root is rejected. Swap this for an
/// S3/Azure Blob implementation without touching the Application layer.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<FileStorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(relativePath);
        var full = ResolveFullPath(normalized);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, content, cancellationToken);
        return normalized;
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var full = ResolveFullPath(Normalize(relativePath));
        return File.Exists(full) ? await File.ReadAllBytesAsync(full, cancellationToken) : null;
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(ResolveFullPath(Normalize(relativePath))));

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var full = ResolveFullPath(Normalize(relativePath));
        if (File.Exists(full))
        {
            File.Delete(full);
        }
        return Task.CompletedTask;
    }

    private static string Normalize(string relativePath)
        => relativePath.Replace('\\', '/').TrimStart('/');

    private string ResolveFullPath(string normalizedRelativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_root, normalizedRelativePath));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Resolved storage path escapes the configured storage root.");
        }
        return full;
    }
}
