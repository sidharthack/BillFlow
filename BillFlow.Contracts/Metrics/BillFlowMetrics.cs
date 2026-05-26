using Prometheus;
using PromMetrics = Prometheus.Metrics;

namespace BillFlow.Contracts.Metrics;

public static class BillFlowMetrics
{
    public static readonly Counter InvoicesCreated =
        PromMetrics.CreateCounter(
            "billflow_invoices_created_total",
            "Total number of invoices created",
            new CounterConfiguration
            {
                LabelNames = ["tenant_id", "currency"]
            });
    public static readonly Counter CustomersCreated =
      PromMetrics.CreateCounter(
          "billflow_customers_created_total",
          "Total number of customers created",
          new CounterConfiguration
          {
              LabelNames = ["tenant_id"]
          });
    public static readonly Counter LoginAttempts =
    PromMetrics.CreateCounter(
        "billflow_login_attempts_total",
        "Total login attempts",
        new CounterConfiguration
        {
            LabelNames = ["outcome"]   // success / failure
        });
    public static readonly Counter InvoiceTransitions =
        PromMetrics.CreateCounter(
            "billflow_invoice_transitions_total",
            "Total invoice status transitions",
            new CounterConfiguration
            {
                LabelNames = ["from_status", "to_status"]
            });
    public static readonly Counter InvoiceValueTotal =
       PromMetrics.CreateCounter(
           "billflow_invoice_value_total",
           "Total value of all invoices created",
           new CounterConfiguration
           {
               LabelNames = ["currency"]
           });
    public static readonly Counter EmailsSent =
    PromMetrics.CreateCounter(
        "billflow_emails_sent_total",
        "Total emails sent",
        new CounterConfiguration
        {
            LabelNames = ["event_type", "status"]
        });
    public static readonly Counter RateLimitRejections =
    PromMetrics.CreateCounter(
        "billflow_rate_limit_rejections_total",
        "Total requests rejected by rate limiter",
        new CounterConfiguration
        {
            LabelNames = ["route"]
        });

    public static readonly Counter GatewayRequestsProxied =
     PromMetrics.CreateCounter(
         "billflow_gateway_requests_proxied_total",
         "Total requests proxied by the gateway",
         new CounterConfiguration
         {
             LabelNames = ["cluster", "status_code"]
         });
    public static readonly Counter EventsConsumed =
    PromMetrics.CreateCounter(
        "billflow_events_consumed_total",
        "Total RabbitMQ events consumed",
        new CounterConfiguration
        {
            LabelNames = ["routing_key", "outcome"]
        });
    public static readonly Histogram PdfGenerationDuration =
        PromMetrics.CreateHistogram(
            "billflow_pdf_generation_seconds",
            "Time taken to generate invoice PDF",
            new HistogramConfiguration
            {
                Buckets = [0.1, 0.25, 0.5, 1.0, 2.0, 5.0]
            });
    public static readonly Histogram OverdueJobDuration =
     PromMetrics.CreateHistogram(
         "billflow_overdue_job_seconds",
         "Duration of the overdue invoice detection job",
         new HistogramConfiguration
         {
             Buckets = [0.1, 0.5, 1.0, 5.0, 10.0, 30.0]
         });
}