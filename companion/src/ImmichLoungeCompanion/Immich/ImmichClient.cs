using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ImmichLoungeCompanion.Immich.Dtos;
using ImmichLoungeCompanion.Models;
using Microsoft.Extensions.Logging;

namespace ImmichLoungeCompanion.Immich;

public class ImmichClient(IHttpClientFactory httpClientFactory, ILogger<ImmichClient> logger) : IImmichClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // NOTE: Do NOT set BaseAddress or DefaultRequestHeaders on the shared named client —
    // the factory may return the same instance across calls and mutations are not thread-safe.
    // Instead, build per-request URIs and set x-api-key per request using HttpRequestMessage.
    private (HttpClient Client, string BaseUrl) BuildClient(ImmichSettings settings)
    {
        var client = httpClientFactory.CreateClient("immich");
        return (client, settings.ServerUrl.TrimEnd('/'));
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string context)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        logger.LogError("Immich API error [{Context}] HTTP {Status}: {Body}",
            context, (int)response.StatusCode, body);
        response.EnsureSuccessStatusCode(); // throws with status code
    }

    private static HttpRequestMessage ApiRequest(HttpMethod method, string baseUrl, string path, string apiKey)
    {
        var req = new HttpRequestMessage(method, $"{baseUrl}/{path}");
        req.Headers.Add("x-api-key", apiKey);
        return req;
    }

    public async Task<(bool Ok, int ImageCount, int VideoCount, string? Error)> TestConnectionAsync(ImmichSettings settings)
    {
        try
        {
            var (client, baseUrl) = BuildClient(settings);

            // Step 1: ping (no auth) — validates the server URL
            var pingReq = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/server/ping");
            var pingResp = await client.SendAsync(pingReq);
            if (!pingResp.IsSuccessStatusCode)
            {
                return (false, 0, 0, $"Server unreachable (HTTP {(int)pingResp.StatusCode})");
            }

            // Step 2: asset statistics (requires auth) — validates API key, returns user's own asset counts
            var statsReq = ApiRequest(HttpMethod.Get, baseUrl, "api/assets/statistics", settings.ApiKey);
            var statsResp = await client.SendAsync(statsReq);
            if (!statsResp.IsSuccessStatusCode)
            {
                return (false, 0, 0, $"HTTP {(int)statsResp.StatusCode}");
            }

            var stats = await statsResp.Content.ReadFromJsonAsync<AssetStatisticsResponse>(JsonOptions);
            return (true, stats?.Images ?? 0, stats?.Videos ?? 0, null);
        }
        catch (Exception ex)
        {
            return (false, 0, 0, ex.Message);
        }
    }

    public async Task<List<ImmichAlbum>> GetAlbumsAsync(ImmichSettings settings)
    {
        var (client, baseUrl) = BuildClient(settings);
        // Shared album listing works, but Immich asset search does not resolve shared
        // album ids yet. Keep the shared-album helpers below parked until that is fixed.
        return await GetAlbumsPageAsync(client, baseUrl, settings.ApiKey, "api/albums", "GET /api/albums");
    }

    public async Task<List<ImmichPerson>> GetPeopleAsync(ImmichSettings settings)
    {
        var (client, baseUrl) = BuildClient(settings);
        var req = ApiRequest(HttpMethod.Get, baseUrl, "api/people", settings.ApiKey);
        var response = await client.SendAsync(req);
        await EnsureSuccessAsync(response, "GET /api/people");
        // Immich returns { people: [...], total, hasNextPage }
        var wrapper = await response.Content.ReadFromJsonAsync<PeopleResponse>(JsonOptions);
        return wrapper?.People ?? [];
    }

    public async Task<List<ImmichTag>> GetTagsAsync(ImmichSettings settings)
    {
        var (client, baseUrl) = BuildClient(settings);
        var req = ApiRequest(HttpMethod.Get, baseUrl, "api/tags", settings.ApiKey);
        var response = await client.SendAsync(req);
        await EnsureSuccessAsync(response, "GET /api/tags");
        return await response.Content.ReadFromJsonAsync<List<ImmichTag>>(JsonOptions) ?? [];
    }

    public async Task<List<ImmichAsset>> SearchAssetsAllPagesAsync(
        ImmichSettings settings, SearchAssetsRequest request)
    {
        var (client, baseUrl) = BuildClient(settings);
        var allAssets = new List<ImmichAsset>();
        request.Page = 1;
        while (true)
        {
            var body = JsonSerializer.Serialize(request, JsonOptions);
            var req = ApiRequest(HttpMethod.Post, baseUrl, "api/search/metadata", settings.ApiKey);
            req.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            var response = await client.SendAsync(req);
            await EnsureSuccessAsync(response, $"POST /api/search/metadata page={request.Page}");
            var result = await response.Content.ReadFromJsonAsync<SearchAssetsResponse>(JsonOptions);
            if (result?.Assets.Items is { Count: > 0 } items)
            {
                allAssets.AddRange(items);
            }

            if (result?.Assets.NextPage == null)
            {
                break;
            }

            request.Page = int.Parse(result.Assets.NextPage);
        }
        return allAssets;
    }

    public async Task<List<ImmichMemory>> GetMemoriesAsync(ImmichSettings settings, DateOnly date)
    {
        var (client, baseUrl) = BuildClient(settings);
        var req = ApiRequest(HttpMethod.Get, baseUrl, $"api/memories?for={date:yyyy-MM-dd}", settings.ApiKey);
        var response = await client.SendAsync(req);
        await EnsureSuccessAsync(response, $"GET /api/memories?for={date:yyyy-MM-dd}");
        return await response.Content.ReadFromJsonAsync<List<ImmichMemory>>(JsonOptions) ?? [];
    }

    private class PeopleResponse
    {
        public List<ImmichPerson> People { get; set; } = [];
        public int Total { get; set; }
    }

    private async Task<List<ImmichAlbum>> GetAlbumsPageAsync(
        HttpClient client,
        string baseUrl,
        string apiKey,
        string path,
        string context)
    {
        var req = ApiRequest(HttpMethod.Get, baseUrl, path, apiKey);
        var response = await client.SendAsync(req);
        await EnsureSuccessAsync(response, context);
        return await response.Content.ReadFromJsonAsync<List<ImmichAlbum>>(JsonOptions) ?? [];
    }

    private async Task<List<ImmichAlbum>> GetSharedAlbumsAsync(HttpClient client, string baseUrl, string apiKey)
    {
        const string path = "api/albums?shared=true";
        const string context = "GET /api/albums?shared=true";

        var req = ApiRequest(HttpMethod.Get, baseUrl, path, apiKey);
        var response = await client.SendAsync(req);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ImmichAlbum>>(JsonOptions) ?? [];
        }

        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
        {
            logger.LogWarning(
                "Immich shared albums query rejected [{Context}] HTTP {Status}: {Body}",
                context, (int)response.StatusCode, body);
            logger.LogWarning("Immich shared albums query is unavailable; continuing with own albums only.");
            return [];
        }

        logger.LogError("Immich API error [{Context}] HTTP {Status}: {Body}",
            context, (int)response.StatusCode, body);
        response.EnsureSuccessStatusCode();
        return [];
    }

    private async Task NormalizeAlbumCountsAsync(HttpClient client, string baseUrl, string apiKey, List<ImmichAlbum> albums)
    {
        foreach (var album in albums)
        {
            if (album.AssetCount > 0)
            {
                continue;
            }

            if (album.Assets.Count > 0)
            {
                album.AssetCount = album.Assets.Count;
                continue;
            }

            var path = $"api/albums/{Uri.EscapeDataString(album.Id)}?withoutAssets=true";
            var req = ApiRequest(HttpMethod.Get, baseUrl, path, apiKey);
            var response = await client.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var detailedAlbum = await response.Content.ReadFromJsonAsync<ImmichAlbum>(JsonOptions);
                if (detailedAlbum is not null)
                {
                    album.AssetCount = detailedAlbum.AssetCount;
                }
                continue;
            }

            var body = await response.Content.ReadAsStringAsync();
            logger.LogWarning(
                "Immich album count query rejected [GET /api/albums/{AlbumId}?withoutAssets=true] HTTP {Status}: {Body}",
                album.Id, (int)response.StatusCode, body);
        }
    }

    private class AssetStatisticsResponse
    {
        public int Images { get; set; }
        public int Videos { get; set; }
    }
}
