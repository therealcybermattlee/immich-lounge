using System.Net;
using System.Net.Http;
using System.Text;
using ImmichLoungeCompanion.Immich;
using ImmichLoungeCompanion.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImmichLoungeCompanion.Tests.Immich;

[TestClass]
public class ImmichClientTests
{
    [TestMethod]
    public async Task GetAlbumsAsync_FetchesOwnAndSharedAlbums_AndDeduplicatesById()
    {
        var requests = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!.ToString());

            return request.RequestUri!.PathAndQuery switch
            {
                "/api/albums" => JsonResponse("""
                    [
                      { "id": "own-1", "albumName": "Own Album", "assetCount": 3 },
                      { "id": "shared-dup", "albumName": "Duplicate From Own", "assetCount": 5 }
                    ]
                    """),
                "/api/albums?shared=true" => JsonResponse("""
                    [
                      { "id": "shared-dup", "albumName": "Duplicate From Shared", "assetCount": 5 },
                      { "id": "shared-1", "albumName": "Shared Album", "assetCount": 7 }
                    ]
                    """),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        });

        var httpClient = new HttpClient(handler);
        var factory = new StubHttpClientFactory(httpClient);
        var client = new ImmichClient(factory, NullLogger<ImmichClient>.Instance);

        var albums = await client.GetAlbumsAsync(new ImmichSettings
        {
            ServerUrl = "http://immich.example",
            ApiKey = "test-key"
        });

        CollectionAssert.AreEqual(
            new[]
            {
                "http://immich.example/api/albums",
                "http://immich.example/api/albums?shared=true"
            },
            requests);

        Assert.AreEqual(3, albums.Count);
        Assert.AreEqual("own-1", albums[0].Id);
        Assert.AreEqual("shared-dup", albums[1].Id);
        Assert.AreEqual("Duplicate From Own", albums[1].AlbumName);
        Assert.AreEqual("shared-1", albums[2].Id);
    }

    [TestMethod]
    public async Task GetAlbumsAsync_WhenSharedQueriesAreRejected_ReturnsOwnAlbumsOnly()
    {
        var handler = new StubHttpMessageHandler(request => request.RequestUri!.PathAndQuery switch
        {
            "/api/albums" => JsonResponse("""
                [
                  { "id": "own-1", "albumName": "Own Album", "assetCount": 3 }
                ]
                """),
            "/api/albums?shared=true" => new HttpResponseMessage(HttpStatusCode.BadRequest),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        var httpClient = new HttpClient(handler);
        var factory = new StubHttpClientFactory(httpClient);
        var client = new ImmichClient(factory, NullLogger<ImmichClient>.Instance);

        var albums = await client.GetAlbumsAsync(new ImmichSettings
        {
            ServerUrl = "http://immich.example",
            ApiKey = "test-key"
        });

        Assert.AreEqual(1, albums.Count);
        Assert.AreEqual("own-1", albums[0].Id);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
