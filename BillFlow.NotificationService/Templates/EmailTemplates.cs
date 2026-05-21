using SendGrid.Helpers.Mail;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace BillFlow.NotificationService.Templates;

public static class EmailTemplates
{
    private static string Wrap(string companyName, string content) => $$"""
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <style>
        body { font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 40px auto; background: #fff;
                     border-radius: 8px; overflow: hidden;
                     box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
        .header { background: #6366F1; padding: 28px 32px; }
        .header h1 { color: #fff; margin: 0; font-size: 22px; font-weight: 600; }
        .header p { color: #c7d2fe; margin: 4px 0 0; font-size: 13px; }
        .body { padding: 32px; color: #374151; line-height: 1.6; }
        .amount-box { background: #f0f0ff; border-left: 4px solid #6366F1;
                      border-radius: 4px; padding: 16px 20px; margin: 20px 0; }
        .amount-box .label { font-size: 12px; color: #6366F1; font-weight: 600;
                             text-transform: uppercase; letter-spacing: 0.5px; }
        .amount-box .value { font-size: 28px; font-weight: 700;
                             color: #1f2937; margin-top: 4px; }
        .detail-row { display: flex; justify-content: space-between;
                      padding: 8px 0; border-bottom: 1px solid #f3f4f6; }
        .detail-row:last-child { border-bottom: none; }
        .detail-label { color: #6b7280; font-size: 13px; }
        .detail-value { color: #1f2937; font-size: 13px; font-weight: 500; }
        .btn { display: inline-block; background: #6366F1; color: #fff;
               padding: 12px 28px; border-radius: 6px; text-decoration: none;
               font-weight: 600; font-size: 14px; margin-top: 20px; }
        .footer { background: #f9fafb; padding: 20px 32px;
                  font-size: 12px; color: #9ca3af; text-align: center; }
      </style>
    </head>
    <body>
      <div class="container">
        <div class="header">
          < h1 >{{companyName}}</ h1 >
          < p > Invoice Management </ p >
        </ div >
        < div class= "body" >{ { content} }</ div >
        < div class= "footer" >
          This email was sent by {{companyName}} via BillFlow.
          Please do not reply to this email.
        </div>
      </div>
    </body>
    </html>
    """;

    public static (string Subject, string Html) InvoiceCreated(
        string companyName,
        string customerName,
        string invoiceNumber,
        decimal totalAmount,
        string currency,
        DateTime createdAt)
    {
        var subject = $"Invoice {invoiceNumber} Created — {companyName}";
        var symbol = GetCurrencySymbol(currency);

        var content = $"""
            <h2 style="margin-top:0;color:#1f2937">New Invoice Created</h2>
            <p>Hi {customerName},</p>
            <p>A new invoice has been created for you by <strong>{companyName}</strong>.</p>

            <div class="amount-box">
              <div class="label">Invoice Total</div>
              <div class="value">{symbol}{totalAmount:N2}</div>
            </div>

            <div class="detail-row">
              <span class="detail-label">Invoice Number</span>
              <span class="detail-value">{invoiceNumber}</span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Date</span>
              <span class="detail-value">{createdAt:dd MMM yyyy}</span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Status</span>
              <span class="detail-value" style="color:#6366F1">Draft</span>
            </div>

            <p style="color:#6b7280;font-size:13px;margin-top:24px">
              You will receive another email when this invoice is sent for payment.
            </p>
            """;

        return (subject, Wrap(companyName, content));
    }

    public static (string Subject, string Html) InvoiceSent(
        string companyName,
        string customerName,
        string invoiceNumber,
        decimal totalAmount,
        string currency,
        DateTime? dueDate)
    {
        var subject = $"Invoice {invoiceNumber} — Payment Due — {companyName}";
        var symbol = GetCurrencySymbol(currency);
        var dueDateStr = dueDate.HasValue
            ? dueDate.Value.ToString("dd MMM yyyy")
            : "Upon receipt";

        var content = $"""
            <h2 style="margin-top:0;color:#1f2937">Invoice Ready for Payment</h2>
            <p>Hi {customerName},</p>
            <p>Please find your invoice from <strong>{companyName}</strong> below.
               Payment is due by <strong>{dueDateStr}</strong>.</p>

            <div class="amount-box">
              <div class="label">Amount Due</div>
              <div class="value">{symbol}{totalAmount:N2}</div>
            </div>

            <div class="detail-row">
              <span class="detail-label">Invoice Number</span>
              <span class="detail-value">{invoiceNumber}</span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Due Date</span>
              <span class="detail-value" style="color:#d97706;font-weight:600">
                {dueDateStr}
              </span>
            </div>

            <p style="margin-top:24px">
              Please contact <strong>{companyName}</strong> for payment instructions
              or if you have any questions about this invoice.
            </p>
            """;

        return (subject, Wrap(companyName, content));
    }

    public static (string Subject, string Html) InvoiceOverdue(
        string companyName,
        string customerName,
        string invoiceNumber,
        decimal totalAmount,
        string currency,
        DateTime dueDate,
        int daysOverdue)
    {
        var subject = $"OVERDUE: Invoice {invoiceNumber} — Action Required — {companyName}";
        var symbol = GetCurrencySymbol(currency);

        var content = $"""
            <h2 style="margin-top:0;color:#dc2626">Invoice Overdue — Action Required</h2>
            <p>Hi {customerName},</p>
            <p>This is a reminder that invoice <strong>{invoiceNumber}</strong> from
               <strong>{companyName}</strong> is now
               <strong style="color:#dc2626">{daysOverdue} day(s) overdue</strong>.</p>

            <div class="amount-box" style="background:#fff5f5;border-color:#dc2626">
              <div class="label" style="color:#dc2626">Overdue Amount</div>
              <div class="value" style="color:#dc2626">{symbol}{totalAmount:N2}</div>
            </div>

            <div class="detail-row">
              <span class="detail-label">Invoice Number</span>
              <span class="detail-value">{invoiceNumber}</span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Was Due</span>
              <span class="detail-value" style="color:#dc2626">
                {dueDate:dd MMM yyyy} ({daysOverdue} days ago)
              </span>
            </div>

            <p style="margin-top:24px;color:#6b7280;font-size:13px">
              Please contact <strong>{companyName}</strong> immediately to arrange payment
              and avoid any further action.
            </p>
            """;

        return (subject, Wrap(companyName, content));
    }

    public static (string Subject, string Html) InvoicePaid(
        string companyName,
        string customerName,
        string invoiceNumber,
        decimal totalAmount,
        string currency,
        DateTime paidAt)
    {
        var subject = $"Payment Received — Invoice {invoiceNumber} — {companyName}";
        var symbol = GetCurrencySymbol(currency);

        var content = $"""
            <h2 style="margin-top:0;color:#059669">Payment Received — Thank You!</h2>
            <p>Hi {customerName},</p>
            <p>We have received your payment for invoice <strong>{invoiceNumber}</strong>.
               Thank you for your prompt payment.</p>

            <div class="amount-box" style="background:#f0fdf4;border-color:#059669">
              <div class="label" style="color:#059669">Amount Paid</div>
              <div class="value" style="color:#059669">{symbol}{totalAmount:N2}</div>
            </div>

            <div class="detail-row">
              <span class="detail-label">Invoice Number</span>
              <span class="detail-value">{invoiceNumber}</span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Payment Date</span>
              <span class="detail-value" style="color:#059669">
                {paidAt:dd MMM yyyy HH:mm} UTC
              </span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Status</span>
              <span class="detail-value" style="color:#059669">✓ Paid in Full</span>
            </div>

            <p style="color:#6b7280;font-size:13px;margin-top:24px">
              This email serves as your payment confirmation. Please keep it for your records.
            </p>
            """;

        return (subject, Wrap(companyName, content));
    }

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "INR" => "₹",
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        _ => currency + " "
    };
}