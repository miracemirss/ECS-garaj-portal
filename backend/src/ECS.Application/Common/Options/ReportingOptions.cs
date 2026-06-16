namespace ECS.Application.Common.Options;

/// <summary>
/// Configuration for generated reports (bound from the <c>Reporting</c> section).
/// Company identity and the VAT rate live here rather than in the database because
/// they are presentation/legal concerns of the printed document. The company name
/// is still overlaid from <c>company_settings</c> at render time when present.
/// </summary>
public sealed class ReportingOptions
{
    public const string SectionName = "Reporting";

    public ReportCompanyInfo Company { get; set; } = new();

    /// <summary>VAT (KDV) rate applied to the cost summary, e.g. 0.20 for 20%.</summary>
    public decimal VatRate { get; set; } = 0.20m;

    public string DefaultCurrency { get; set; } = "TRY";

    /// <summary>Optional absolute/relative path to a PNG/JPG logo. Falls back to a typographic mark when missing.</summary>
    public string? LogoPath { get; set; }

    /// <summary>Storage sub-folder (under the file-storage root) for generated report PDFs.</summary>
    public string StorageFolder { get; set; } = "reports";

    /// <summary>Base URL embedded in the QR code so a scanned report links back to its verification page.</summary>
    public string VerificationBaseUrl { get; set; } = "https://portal.ecs.local/reports/verify";
}

public sealed class ReportCompanyInfo
{
    public string Name { get; set; } = "ECS Europe Cargo Service";
    public string? LegalName { get; set; } = "ECS Europe Cargo Service Lojistik A.Ş.";
    public string? Address { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
}
