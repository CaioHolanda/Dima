using System.Text.Json;
using Dima.Core.Responses;

namespace Dima.Web.Handlers;

internal static class HttpResponseReader
{
    public static async Task<Response<T>> ReadAsync<T>(HttpResponseMessage response, string message)
    {
        var result = await DeserializeAsync<Response<T>>(response);
        if (result is null || (response.IsSuccessStatusCode && !result.IsSuccess)) return new Response<T>(default, FailureCode(response), message);
        result.Code = (int)response.StatusCode;
        if (!response.IsSuccessStatusCode)
        {
            result.Data = default;
            if (string.IsNullOrWhiteSpace(result.Message)) result.Message = message;
        }
        return result;
    }

    public static async Task<PagedResponse<T>> ReadPagedAsync<T>(HttpResponseMessage response, string message)
    {
        var result = await DeserializeAsync<PagedResponse<T>>(response);
        if (result is null || (response.IsSuccessStatusCode && !result.IsSuccess)) return new PagedResponse<T>(default, FailureCode(response), message);
        result.Code = (int)response.StatusCode;
        if (!response.IsSuccessStatusCode)
        {
            result.Data = default;
            if (string.IsNullOrWhiteSpace(result.Message)) result.Message = message;
        }
        return result;
    }

    public static async Task<Response<T>> GetResponseAsync<T>(this HttpClient client, string url, string message)
    {
        using var response = await client.GetAsync(url);
        return await ReadAsync<T>(response, message);
    }

    public static async Task<PagedResponse<T>> GetPagedResponseAsync<T>(this HttpClient client, string url, string message)
    {
        using var response = await client.GetAsync(url);
        return await ReadPagedAsync<T>(response, message);
    }

    private static int FailureCode(HttpResponseMessage response)
        => response.IsSuccessStatusCode ? 502 : (int)response.StatusCode;

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        try
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            // ProblemDetails and unrelated JSON are not application response envelopes.
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.EnumerateObject().Any(property =>
                    property.Name.Equals("code", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out _)))
                return default;
            return document.RootElement.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException) { return default; }
        catch (NotSupportedException) { return default; }
    }
}
