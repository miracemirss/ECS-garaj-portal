using ECS.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace ECS.Infrastructure.Pdf;

/// <summary>
/// Loads and caches the report logo once at startup. When no logo file is
/// configured/found, <see cref="Logo"/> is null and the document falls back to a
/// typographic ECS mark, so reports never break on a missing asset.
/// </summary>
public sealed class ReportBrandAssets
{
    public ReportBrandAssets(IOptions<ReportingOptions> options)
    {
        var path = options.Value.LogoPath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                Logo = File.ReadAllBytes(path);
            }
            catch
            {
                Logo = null;
            }
        }
    }

    public byte[]? Logo { get; }
}
