using System.Globalization;
using ECS.Application.Features.Reports.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ECS.Infrastructure.Pdf;

/// <summary>
/// Corporate A4 maintenance/repair report. Pure renderer over
/// <see cref="MaintenanceReportModel"/>: white background, navy headings, tabular
/// layout, repeating header/footer with page numbers, a QR verification block and
/// signature lines. Printable and archive-friendly.
/// </summary>
public sealed class MaintenanceReportDocument : IDocument
{
    // Brand palette (hex strings are accepted by QuestPDF 2024.x color APIs).
    private const string Navy = "#0B2A5B";
    private const string NavySoft = "#13386E";
    private const string Ink = "#1A1A1A";
    private const string Muted = "#5B6470";
    private const string Line = "#D9DEE6";
    private const string Zebra = "#F4F6F9";
    private const string White = "#FFFFFF";

    private static readonly NumberFormatInfo MoneyFormat = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 2,
        NumberGroupSizes = [3]
    };

    private readonly MaintenanceReportModel _m;
    private readonly byte[]? _logo;
    private readonly byte[]? _qr;

    public MaintenanceReportDocument(MaintenanceReportModel model, byte[]? logo, byte[]? qr)
    {
        _m = model;
        _logo = logo is { Length: > 0 } ? logo : null;
        _qr = qr is { Length: > 0 } ? qr : null;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{_m.Header.Title} - {_m.Header.WorkOrderNo}",
        Author = _m.Company.Name,
        Subject = "Araç Bakım / Tamir Raporu",
        Creator = _m.Company.Name
    };

    public DocumentSettings GetSettings() => new();

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.4f, Unit.Centimetre);
            page.PageColor(White);
            page.DefaultTextStyle(t => t.FontSize(9).FontColor(Ink).LineHeight(1.25f));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(8).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ----- Header (repeats on every page) ------------------------------------

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(116).Height(46).Element(ComposeLogo);
                row.RelativeItem().PaddingLeft(12).Column(c =>
                {
                    c.Item().Text(_m.Company.Name).FontSize(16).Bold().FontColor(Navy);
                    c.Item().Text(_m.Header.Title).FontSize(11).SemiBold().FontColor(NavySoft);
                    if (!string.IsNullOrWhiteSpace(_m.Company.LegalName))
                    {
                        c.Item().Text(_m.Company.LegalName!).FontSize(7.5f).FontColor(Muted);
                    }
                });
                row.ConstantItem(150).Element(ComposeHeaderMeta);
            });
            col.Item().PaddingTop(6).LineHorizontal(1.4f).LineColor(Navy);
        });
    }

    private void ComposeLogo(IContainer container)
    {
        if (_logo is not null)
        {
            container.Image(_logo).FitArea();
            return;
        }

        container.Background(Navy).Padding(6).Column(c =>
        {
            c.Item().Text("ECS").FontSize(20).Bold().FontColor(White);
            c.Item().Text("EUROPE CARGO SERVICE").FontSize(5.5f).FontColor(White);
        });
    }

    private void ComposeHeaderMeta(IContainer container)
    {
        container.AlignRight().Column(c =>
        {
            MetaLine(c, "Rapor No", _m.Header.ReportNo);
            MetaLine(c, "İş Emri No", _m.Header.WorkOrderNo);
            MetaLine(c, "Tarih", FormatDate(_m.Header.ReportDateUtc));
        });
    }

    private static void MetaLine(ColumnDescriptor col, string label, string value)
    {
        col.Item().Text(t =>
        {
            t.Span($"{label}: ").FontSize(7.5f).FontColor(Muted);
            t.Span(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(8.5f).SemiBold().FontColor(Navy);
        });
    }

    // ----- Content -----------------------------------------------------------

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(10);
            col.Item().Element(ComposeInfoBand);
            col.Item().Element(ComposeAssetSection);
            col.Item().Element(ComposeWorkOrderSection);
            if (_m.Vehicle is not null)
            {
                col.Item().Element(ComposeOdometerSection);
            }
            col.Item().Element(ComposeTasksSection);
            col.Item().Element(ComposePartsSection);
            col.Item().Element(ComposeCostSection);
            if (!string.IsNullOrWhiteSpace(_m.NextMaintenanceRecommendation))
            {
                col.Item().Element(ComposeRecommendation);
            }
            col.Item().Element(ComposeSignatures);
        });
    }

    private void ComposeInfoBand(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => Card(c, "Firma Bilgileri", inner =>
            {
                KeyValue(inner, "Ünvan", _m.Company.LegalName ?? _m.Company.Name);
                if (!string.IsNullOrWhiteSpace(_m.Company.Address))
                {
                    KeyValue(inner, "Adres", _m.Company.Address!);
                }
                var tax = JoinNonEmpty(" / ", _m.Company.TaxOffice, _m.Company.TaxNo);
                if (!string.IsNullOrWhiteSpace(tax))
                {
                    KeyValue(inner, "Vergi D./No", tax);
                }
                if (!string.IsNullOrWhiteSpace(_m.Company.Phone))
                {
                    KeyValue(inner, "Telefon", _m.Company.Phone!);
                }
                if (!string.IsNullOrWhiteSpace(_m.Company.Email))
                {
                    KeyValue(inner, "E-posta", _m.Company.Email!);
                }
                if (!string.IsNullOrWhiteSpace(_m.Company.Website))
                {
                    KeyValue(inner, "Web", _m.Company.Website!);
                }
            }));

            row.ConstantItem(12);

            row.RelativeItem().Element(c => Card(c, "Rapor Bilgileri", inner =>
            {
                inner.Item().Row(r =>
                {
                    r.RelativeItem().Column(kv =>
                    {
                        kv.Spacing(2);
                        KeyValue(kv, "Rapor No", _m.Header.ReportNo);
                        KeyValue(kv, "Rapor Tarihi", FormatDate(_m.Header.ReportDateUtc));
                        KeyValue(kv, "İş Emri No", _m.Header.WorkOrderNo);
                        KeyValue(kv, "Durum", _m.WorkOrder.Status);
                    });
                    if (_qr is not null)
                    {
                        r.ConstantItem(72).AlignTop().Column(q =>
                        {
                            q.Item().Height(64).AlignRight().Image(_qr).FitArea();
                            q.Item().AlignRight().Text("Doğrulama").FontSize(6.5f).FontColor(Muted);
                        });
                    }
                });
            }));
        });
    }

    private void ComposeAssetSection(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => Card(c, "Araç Bilgileri", inner =>
            {
                if (_m.Vehicle is { } v)
                {
                    KeyValue(inner, "Plaka", v.Plate);
                    KeyValue(inner, "Marka", v.Brand);
                    KeyValue(inner, "Model", v.Model ?? "-");
                    KeyValue(inner, "Şasi No", v.Vin ?? "-");
                    KeyValue(inner, "Güncel KM", $"{FormatInt(v.CurrentOdometerKm)} km");
                }
                else
                {
                    inner.Item().Text("Bu iş emri bir dorseye aittir.").FontColor(Muted).Italic();
                }
            }));

            if (_m.Trailer is { } tr)
            {
                row.ConstantItem(12);
                row.RelativeItem().Element(c => Card(c, "Dorse Bilgileri", inner =>
                {
                    KeyValue(inner, "Plaka", tr.Plate);
                    KeyValue(inner, "Tip", tr.Type ?? "-");
                    KeyValue(inner, "Şasi No", tr.Vin ?? "-");
                }));
            }
        });
    }

    private void ComposeWorkOrderSection(IContainer container)
    {
        container.Element(c => Card(c, "İş Emri Bilgileri", inner =>
        {
            inner.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Spacing(2);
                    KeyValue(left, "İşlem Tipi", _m.WorkOrder.MaintenanceType);
                    KeyValue(left, "Servise Giriş", FormatDate(_m.WorkOrder.ServiceInAtUtc));
                    KeyValue(left, "Tamamlanma", FormatDate(_m.WorkOrder.CompletedAtUtc));
                });
                row.ConstantItem(12);
                row.RelativeItem().Column(right =>
                {
                    right.Spacing(2);
                    KeyValue(right, "Serviste Süre", _m.WorkOrder.DurationText);
                    KeyValue(right, "Usta / Servis", WorkOrderResponsible());
                });
            });

            if (!string.IsNullOrWhiteSpace(_m.WorkOrder.Description))
            {
                inner.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Açıklama: ").FontColor(Muted);
                    t.Span(_m.WorkOrder.Description!);
                });
            }
        }));
    }

    private void ComposeOdometerSection(IContainer container)
    {
        container.Element(c => Card(c, "Kilometre Bilgileri", inner =>
        {
            inner.Item().Row(row =>
            {
                row.Spacing(8);
                row.RelativeItem().Element(x => Stat(x, "Bakım Öncesi KM", FormatKm(_m.Odometer.BeforeKm)));
                row.RelativeItem().Element(x => Stat(x, "Bakım Sonrası KM", FormatKm(_m.Odometer.AfterKm)));
                row.RelativeItem().Element(x => Stat(x, "KM Farkı", FormatKm(_m.Odometer.DifferenceKm)));
            });
        }));
    }

    private void ComposeTasksSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "Yapılan İşlemler"));

            if (_m.Tasks.Count == 0)
            {
                col.Item().Element(c => EmptyNote(c, "Bu iş emri için kayıtlı işlem bulunmuyor."));
                return;
            }

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(28);
                    c.RelativeColumn();
                    c.ConstantColumn(80);
                    c.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "#", Align.Center);
                    HeaderCell(header.Cell(), "Açıklama");
                    HeaderCell(header.Cell(), "İşçilik (saat)", Align.Right);
                    HeaderCell(header.Cell(), "Durum", Align.Center);
                });

                var i = 0;
                foreach (var t in _m.Tasks)
                {
                    var bg = i++ % 2 == 1 ? Zebra : White;
                    BodyCell(table.Cell(), t.Index.ToString(), bg, Align.Center);
                    BodyCell(table.Cell(), t.Description, bg);
                    BodyCell(table.Cell(), t.LaborHours?.ToString("#,##0.##", MoneyFormat) ?? "-", bg, Align.Right);
                    BodyCell(table.Cell(), t.IsCompleted ? "Tamam" : "Bekliyor", bg, Align.Center);
                }
            });
        });
    }

    private void ComposePartsSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "Kullanılan Parçalar"));

            if (_m.Parts.Count == 0)
            {
                col.Item().Element(c => EmptyNote(c, "Bu iş emrinde parça kullanılmadı."));
                return;
            }

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(26);
                    c.ConstantColumn(72);
                    c.RelativeColumn();
                    c.ConstantColumn(52);
                    c.ConstantColumn(40);
                    c.ConstantColumn(72);
                    c.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "#", Align.Center);
                    HeaderCell(header.Cell(), "Parça No");
                    HeaderCell(header.Cell(), "Parça Adı");
                    HeaderCell(header.Cell(), "Miktar", Align.Right);
                    HeaderCell(header.Cell(), "Birim", Align.Center);
                    HeaderCell(header.Cell(), "Birim Fiyat", Align.Right);
                    HeaderCell(header.Cell(), "Tutar", Align.Right);
                });

                var i = 0;
                foreach (var p in _m.Parts)
                {
                    var bg = i++ % 2 == 1 ? Zebra : White;
                    BodyCell(table.Cell(), p.Index.ToString(), bg, Align.Center);
                    BodyCell(table.Cell(), p.PartNo, bg);
                    BodyCell(table.Cell(), p.Name, bg);
                    BodyCell(table.Cell(), p.Quantity.ToString("#,##0.###", MoneyFormat), bg, Align.Right);
                    BodyCell(table.Cell(), p.Unit, bg, Align.Center);
                    BodyCell(table.Cell(), FormatMoney(p.UnitCost), bg, Align.Right);
                    BodyCell(table.Cell(), FormatMoney(p.LineTotal), bg, Align.Right);
                }
            });
        });
    }

    private void ComposeCostSection(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionHeader(c, "Maliyet Özeti"));
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(252).Border(0.75f).BorderColor(Line).Column(box =>
                {
                    CostRow(box, "Parça Toplamı", _m.Cost.PartsTotal);
                    CostRow(box, "İşçilik", _m.Cost.LaborCost);
                    CostRow(box, "Diğer Giderler", _m.Cost.OtherCost);
                    box.Item().PaddingHorizontal(8).LineHorizontal(0.5f).LineColor(Line);
                    CostRow(box, "Ara Toplam", _m.Cost.Subtotal);
                    CostRow(box, $"KDV (%{(_m.Cost.VatRate * 100m).ToString("#,##0.##", MoneyFormat)})", _m.Cost.VatAmount);
                    box.Item().Background(Navy).PaddingVertical(5).PaddingHorizontal(8).Row(r =>
                    {
                        r.RelativeItem().Text("Genel Toplam").FontColor(White).Bold().FontSize(10);
                        r.AutoItem().Text($"{FormatMoney(_m.Cost.GrandTotal)} {_m.Cost.Currency}").FontColor(White).Bold().FontSize(10);
                    });
                });
            });
        });
    }

    private static void CostRow(ColumnDescriptor col, string label, decimal value)
    {
        col.Item().PaddingHorizontal(8).PaddingVertical(2).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(9).FontColor(Muted);
            r.AutoItem().Text(FormatMoney(value)).FontSize(9).SemiBold();
        });
    }

    private void ComposeRecommendation(IContainer container)
    {
        container.Background(Zebra).Border(0.5f).BorderColor(Line).Padding(8).Text(t =>
        {
            t.Span("Sonraki Bakım Önerisi: ").SemiBold().FontColor(Navy);
            t.Span(_m.NextMaintenanceRecommendation!);
        });
    }

    private void ComposeSignatures(IContainer container)
    {
        container.PaddingTop(16).Row(row =>
        {
            row.Spacing(14);
            SignatureBox(row.RelativeItem(), "Usta / Teknisyen", _m.WorkOrder.Technician);
            SignatureBox(row.RelativeItem(), "Servis Sorumlusu", _m.WorkOrder.ServiceProvider);
            SignatureBox(row.RelativeItem(), "Araç Yetkilisi / Şoför", null);
        });
    }

    private static void SignatureBox(IContainer container, string role, string? name)
    {
        container.Column(col =>
        {
            col.Item().Height(44);
            col.Item().LineHorizontal(0.75f).LineColor(Muted);
            col.Item().PaddingTop(2).Text(role).SemiBold().FontSize(8.5f).FontColor(Navy);
            col.Item().Text(string.IsNullOrWhiteSpace(name) ? "Ad Soyad / İmza" : name!).FontSize(8).FontColor(Muted);
        });
    }

    // ----- Footer (repeats on every page) ------------------------------------

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(3).LineHorizontal(0.5f).LineColor(Line);
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span(_m.Company.LegalName ?? _m.Company.Name).FontSize(7.5f).FontColor(Muted);
                    t.Span("  |  Bu rapor sistem tarafından otomatik üretilmiştir.").FontSize(7.5f).FontColor(Muted);
                });
                row.AutoItem().Text(t =>
                {
                    t.Span("Sayfa ").FontSize(8).FontColor(Muted);
                    t.CurrentPageNumber().FontSize(8).SemiBold().FontColor(Navy);
                    t.Span(" / ").FontSize(8).FontColor(Muted);
                    t.TotalPages().FontSize(8).SemiBold().FontColor(Navy);
                });
            });
        });
    }

    // ----- Reusable building blocks ------------------------------------------

    private static void Card(IContainer container, string title, Action<ColumnDescriptor> body)
    {
        container.Border(0.75f).BorderColor(Line).Column(col =>
        {
            col.Item().Background(Navy).PaddingVertical(3).PaddingHorizontal(8)
                .Text(title).FontColor(White).SemiBold().FontSize(9.5f);
            col.Item().PaddingVertical(6).PaddingHorizontal(8).Column(inner =>
            {
                inner.Spacing(2);
                body(inner);
            });
        });
    }

    private static void KeyValue(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(96).Text(label).FontColor(Muted).FontSize(8.5f);
            row.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).SemiBold().FontSize(8.5f);
        });
    }

    private static void Stat(IContainer container, string label, string value)
    {
        container.Background(Zebra).Border(0.5f).BorderColor(Line).Padding(8).Column(c =>
        {
            c.Item().Text(label).FontColor(Muted).FontSize(8);
            c.Item().Text(value).FontColor(Navy).Bold().FontSize(13);
        });
    }

    private static void SectionHeader(IContainer container, string title)
        => container.BorderBottom(1.2f).BorderColor(Navy).PaddingBottom(2)
            .Text(title).FontColor(Navy).Bold().FontSize(10.5f);

    private static void EmptyNote(IContainer container, string text)
        => container.PaddingVertical(4).Text(text).FontColor(Muted).Italic().FontSize(8.5f);

    private enum Align { Left, Center, Right }

    private static void HeaderCell(IContainer cell, string text, Align align = Align.Left)
    {
        var c = cell.Background(Navy).PaddingVertical(4).PaddingHorizontal(6);
        c = align switch { Align.Right => c.AlignRight(), Align.Center => c.AlignCenter(), _ => c };
        c.Text(text).FontColor(White).SemiBold().FontSize(8.5f);
    }

    private static void BodyCell(IContainer cell, string text, string background, Align align = Align.Left)
    {
        var c = cell.Background(background).BorderBottom(0.5f).BorderColor(Line).PaddingVertical(3).PaddingHorizontal(6);
        c = align switch { Align.Right => c.AlignRight(), Align.Center => c.AlignCenter(), _ => c };
        c.Text(text).FontSize(8.5f);
    }

    // ----- Formatting helpers ------------------------------------------------

    private string WorkOrderResponsible()
    {
        var value = JoinNonEmpty(" / ", _m.WorkOrder.Technician, _m.WorkOrder.ServiceProvider);
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string JoinNonEmpty(string separator, params string?[] values)
        => string.Join(separator, values.Where(v => !string.IsNullOrWhiteSpace(v)));

    private static string FormatMoney(decimal value) => value.ToString("#,##0.00", MoneyFormat);

    private static string FormatInt(int value) => value.ToString("#,##0", MoneyFormat);

    private static string FormatKm(int? value) => value is { } v ? $"{FormatInt(v)} km" : "-";

    private static string FormatDate(DateTime? utc)
    {
        if (utc is null)
        {
            return "-";
        }

        var value = utc.Value;
        try
        {
            var istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            value = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), istanbul);
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to the raw (UTC) value when tz data is unavailable.
        }

        return value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
