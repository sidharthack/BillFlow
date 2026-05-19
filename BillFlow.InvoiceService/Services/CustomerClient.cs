using System.Net.Http.Headers;
using System.Text.Json;

namespace BillFlow.InvoiceService.Services;

public class CustomerClient : ICustomerClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CustomerClient> _logger;

    public CustomerClient(
        IHttpClientFactory httpClientFactory,
        ILogger<CustomerClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<CustomerSnapshot?> GetCustomerAsync(
        int customerId, string bearerToken)
    {
        var client = _httpClientFactory.CreateClient("CustomerService");

        // Forward the caller's JWT so CustomerService can authenticate + scope
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await client.GetAsync($"/customer/{customerId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CustomerService returned {StatusCode} for customer {Id}",
                    response.StatusCode, customerId);
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            return new CustomerSnapshot(
                root.GetProperty("id").GetInt32(),
                root.GetProperty("name").GetString()!,
                root.GetProperty("email").GetString()!,
                root.TryGetProperty("gstNumber", out var gst)
                    ? gst.GetString()
                    : null
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Failed to reach CustomerService for customer {Id}", customerId);
            return null;
        }
    }
}