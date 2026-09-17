namespace Dima.Api.Configuration;

// Preserve the existing root configuration keys used by deployments.
public sealed class ApiOptions
{
    public string BackendUrl { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = string.Empty;
    public string StripeApiKey { get; set; } = string.Empty;
    public string StripeWebhookSecret { get; set; } = string.Empty;
}
