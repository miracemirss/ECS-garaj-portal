namespace ECS.Infrastructure.Storage;

/// <summary>Options for the filesystem-backed <see cref="LocalFileStorage"/> (bound from the <c>FileStorage</c> section).</summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Root directory under which all generated files are stored. Created on startup if absent.</summary>
    public string RootPath { get; set; } = "storage";
}
