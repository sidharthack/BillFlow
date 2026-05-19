using BillFlow.InvoiceService.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillFlow.InvoiceService.Documents;

public class InvoicePdfDocument : IDocument
{
    private readonly InvoiceResponse _invoice;
    private readonly TenantBranding _branding;

    // Colors
    private static readonly string _primary = "#6366F1";     // indigo
    private static readonly string _lightGray = "#F8F9FA";
    private static readonly string _darkText = "#1A1A2E";
    private static readonly string _mutedText = "#6C757D";

    public InvoicePdfDocument(InvoiceResponse invoice, TenantBranding branding)
    {
        _invoice = invoice;
        _branding = branding;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Invoice {_invoice.InvoiceNumber}",
        Author = _branding.CompanyName,
        CreationDate = DateTimeOffset.UtcNow
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x
                .FontFamily("Arial")
                .FontSize(10)
                .FontColor(_darkText));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── Header ────────────────────────────────────────────────────────────

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Top bar with company name and INVOICE label
            col.Item().Row(row =>
            {
                // Company name (left)
                row.RelativeItem().Column(c =>
                {
                    c.Item()
                     .Text(_branding.CompanyName)
                     .Bold()
                     .FontSize(20)
                     .FontColor(_primary);

                    if (!string.IsNullOrWhiteSpace(_branding.CompanyEmail))
                        c.Item()
                         .Text(_branding.CompanyEmail)
                         .FontSize(9)
                         .FontColor(_mutedText);

                    if (!string.IsNullOrWhiteSpace(_branding.CompanyPhone))
                        c.Item()
                         .Text(_branding.CompanyPhone)
                         .FontSize(9)
                         .FontColor(_mutedText);
                });

                // INVOICE label + number (right)
                row.ConstantItem(180).Column(c =>
                {
                    c.Item()
                     .AlignRight()
                     .Text("INVOICE")
                     .Bold()
                     .FontSize(26)
                     .FontColor(_primary);

                    c.Item()
                     .AlignRight()
                     .Text(_invoice.InvoiceNumber)
                     .FontSize(11)
                     .FontColor(_mutedText);
                });
            });

            // Divider
            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(_primary);
        });
    }

    // ── Content ───────────────────────────────────────────────────────────

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(16);

            // Bill To + Invoice Details side by side
            col.Item().Row(row =>
            {
                // Bill To (left)
                row.RelativeItem().Column(c =>
                {
                    c.Item()
                     .Text("BILL TO")
                     .Bold()
                     .FontSize(9)
                     .FontColor(_mutedText)
                     .LetterSpacing(1);

                    c.Item().PaddingTop(4).Text(_invoice.CustomerName).Bold();
                    c.Item().Text(_invoice.CustomerEmail).FontColor(_mutedText);

                    if (!string.IsNullOrWhiteSpace(_invoice.CustomerGstNumber))
                        c.Item()
                         .Text($"GST: {_invoice.CustomerGstNumber}")
                         .FontColor(_mutedText);
                });

                // Invoice Details (right)
                row.ConstantItem(200).Column(c =>
                {
                    c.Item()
                     .Text("INVOICE DETAILS")
                     .Bold()
                     .FontSize(9)
                     .FontColor(_mutedText)
                     .LetterSpacing(1);

                    c.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("Issue Date:").FontColor(_mutedText);
                        r.ConstantItem(100)
                         .AlignRight()
                         .Text(_invoice.CreatedAt.ToString("dd MMM yyyy"));
                    });

                    if (_invoice.DueDate.HasValue)
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Due Date:").FontColor(_mutedText);
                            r.ConstantItem(100)
                             .AlignRight()
                             .Text(_invoice.DueDate.Value.ToString("dd MMM yyyy"))
                             .Bold()
                             .FontColor(_primary);
                        });

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Status:").FontColor(_mutedText);
                        r.ConstantItem(100)
                         .AlignRight()
                         .Text(_invoice.Status.ToUpperInvariant())
                         .Bold()
                         .FontColor(GetStatusColor(_invoice.Status));
                    });

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Currency:").FontColor(_mutedText);
                        r.ConstantItem(100)
                         .AlignRight()
                         .Text(_invoice.Currency);
                    });
                });
            });

            // Line items table
            col.Item().Element(ComposeLineItemsTable);

            // Totals
            col.Item().Element(ComposeTotals);

            // Notes
            if (!string.IsNullOrWhiteSpace(_invoice.Notes))
                col.Item().Element(ComposeNotes);
        });
    }

    // ── Line items table ──────────────────────────────────────────────────

    // ── Line items table ──────────────────────────────────────────────────

    private void ComposeLineItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            // Column definitions
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(4);   // Description
                cols.RelativeColumn(1);   // Qty
                cols.RelativeColumn(1.5f); // Unit Price
                cols.RelativeColumn(1.5f); // Amount
            });

            // Header row
            table.Header(header =>
            {
                void HeaderCell(string text, bool alignRight = false)
                {
                    header.Cell()
                          .Background(_primary)
                          .Padding(8)
                          .Element(c =>
                          {
                              var t = c.Text(text)
                                       .Bold()
                                       .FontSize(9)
                                       .FontColor(Colors.White)
                                       .LetterSpacing(0.5f);

                              if (alignRight)
                                  t.AlignRight();
                          });
                }

                HeaderCell("DESCRIPTION");
                HeaderCell("QTY", alignRight: true);
                HeaderCell("UNIT PRICE", alignRight: true);
                HeaderCell("AMOUNT", alignRight: true);
            });

            // Data rows
            var isAlternate = false;
            foreach (var item in _invoice.LineItems)
            {
                var bg = isAlternate ? _lightGray : Colors.White.ToString();
                isAlternate = !isAlternate;

                table.Cell()
                     .Background(bg)
                     .Padding(8)
                     .Text(item.Description);

                table.Cell()
                     .Background(bg)
                     .Padding(8)
                     .AlignRight()
                     .Text(item.Quantity.ToString());

                table.Cell()
                     .Background(bg)
                     .Padding(8)
                     .AlignRight()
                     .Text(FormatCurrency(item.UnitPrice));

                table.Cell()
                     .Background(bg)
                     .Padding(8)
                     .AlignRight()
                     .Text(FormatCurrency(item.Amount));
            }
        });
    }
    // ── Totals ────────────────────────────────────────────────────────────

    private void ComposeTotals(IContainer container)
    {
        container.AlignRight().Column(col =>
        {
            col.Item().Width(250).Column(inner =>
            {
                inner.Spacing(4);

                // Subtotal row
                inner.Item().Row(row =>
                {
                    row.RelativeItem()
                       .Text("Subtotal")
                       .FontColor(_mutedText);
                    row.ConstantItem(100)
                       .AlignRight()
                       .Text(FormatCurrency(_invoice.SubTotal));
                });

                // Tax row
                inner.Item().Row(row =>
                {
                    row.RelativeItem()
                       .Text($"Tax ({_invoice.TaxRate:P0})")
                       .FontColor(_mutedText);
                    row.ConstantItem(100)
                       .AlignRight()
                       .Text(FormatCurrency(_invoice.TaxAmount));
                });

                // Divider
                inner.Item().LineHorizontal(0.5f).LineColor("#DEE2E6");

                // Total row — highlighted
                inner.Item()
                     .Background(_primary)
                     .Padding(8)
                     .Row(row =>
                     {
                         row.RelativeItem()
                            .Text("TOTAL")
                            .Bold()
                            .FontColor(Colors.White);
                         row.ConstantItem(100)
                            .AlignRight()
                            .Text(FormatCurrency(_invoice.TotalAmount))
                            .Bold()
                            .FontColor(Colors.White);
                     });
            });
        });
    }

    // ── Notes ─────────────────────────────────────────────────────────────

    private void ComposeNotes(IContainer container)
    {
        container.Column(col =>
        {
            col.Item()
               .Text("NOTES")
               .Bold()
               .FontSize(9)
               .FontColor(_mutedText)
               .LetterSpacing(1);

            col.Item()
               .PaddingTop(4)
               .Background(_lightGray)
               .Padding(10)
               .Text(_invoice.Notes!)
               .FontColor(_mutedText);
        });
    }

    // ── Footer ────────────────────────────────────────────────────────────

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor("#DEE2E6");

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem()
                   .Text($"Generated by {_branding.CompanyName} · BillFlow")
                   .FontSize(8)
                   .FontColor(_mutedText);

                row.ConstantItem(60)
                   .AlignRight()
                   .Text(x =>
                   {
                       x.Span("Page ").FontSize(8).FontColor(_mutedText);
                       x.CurrentPageNumber().FontSize(8).FontColor(_mutedText);
                       x.Span(" of ").FontSize(8).FontColor(_mutedText);
                       x.TotalPages().FontSize(8).FontColor(_mutedText);
                   });
            });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private string FormatCurrency(decimal amount)
    {
        var symbol = _invoice.Currency switch
        {
            "INR" => "₹",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => _invoice.Currency + " "
        };
        return $"{symbol}{amount:N2}";
    }

    private static string GetStatusColor(string status) => status switch
    {
        "Paid" => "#28A745",
        "Overdue" => "#DC3545",
        "Cancelled" => "#6C757D",
        "Sent" => "#FFC107",
        _ => "#6366F1"
    };
}

// Tenant branding passed into the PDF
public record TenantBranding(
    string CompanyName,
    string? CompanyEmail,
    string? CompanyPhone,
    string PrimaryColor
);