using ImmichLoungeCompanion.Immich;
using ImmichLoungeCompanion.Immich.Dtos;
using ImmichLoungeCompanion.Models;
using ImmichLoungeCompanion.Playlist;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace ImmichLoungeCompanion.Tests.Playlist;

[TestClass]
public class PlaylistAssetCollectorTests
{
    private static ImmichSettings FakeImmich => new() { ServerUrl = "http://immich", ApiKey = "k" };

    [TestMethod]
    public async Task CollectAsync_AddsMemoriesOutsideRuleTree()
    {
        var client = Substitute.For<IImmichClient>();
        client.SearchAssetsAllPagesAsync(FakeImmich, Arg.Any<SearchAssetsRequest>())
            .Returns([new ImmichAsset { Id = "album-1", Type = "IMAGE" }]);
        client.GetMemoriesAsync(FakeImmich, Arg.Any<DateOnly>())
            .Returns([
                new ImmichMemory
                {
                    Assets =
                    [
                        new ImmichAsset { Id = "memory-1", Type = "IMAGE" }
                    ]
                }
            ]);

        var collector = new PlaylistAssetCollector(client);
        var profile = new Profile
        {
            ContentSources =
            [
                new() { Type = "album", Id = "a1", Label = "Album" },
                new() { Type = "memories", Id = "", Label = "Memories" }
            ]
        };

        var assets = await collector.CollectAsync(profile, FakeImmich);

        Assert.AreEqual(2, assets.Count);
        Assert.IsTrue(assets.ContainsKey("album-1"));
        Assert.IsTrue(assets.ContainsKey("memory-1"));
        Assert.IsNull(assets["memory-1"].SourceLabel);
    }

    [TestMethod]
    public async Task CollectAsync_SearchSourceUsesSmartSearchWithDateBounds()
    {
        var client = Substitute.For<IImmichClient>();
        SmartSearchRequest? capturedRequest = null;
        client.SmartSearchAllPagesAsync(FakeImmich, Arg.Any<SmartSearchRequest>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.Arg<SmartSearchRequest>();
                return [new ImmichAsset { Id = "search-1", Type = "IMAGE" }];
            });

        var collector = new PlaylistAssetCollector(client);
        var profile = new Profile
        {
            ContentSources = [new() { Type = "search", Id = "beach sunset", Label = "beach sunset" }],
            DateFilter = new() { Type = "range", From = "2024-01-01", To = "2024-12-31" }
        };

        var assets = await collector.CollectAsync(profile, FakeImmich);

        Assert.AreEqual(1, assets.Count);
        Assert.AreEqual("beach sunset", assets["search-1"].SourceLabel);
        Assert.IsNotNull(capturedRequest);
        Assert.AreEqual("beach sunset", capturedRequest.Query);
        // Immich requires full ISO datetimes; bare dates get HTTP 400.
        Assert.AreEqual("2024-01-01T00:00:00.000Z", capturedRequest.TakenAfter);
        Assert.AreEqual("2024-12-31T23:59:59.999Z", capturedRequest.TakenBefore);
        await client.DidNotReceive().SearchAssetsAllPagesAsync(FakeImmich, Arg.Any<SearchAssetsRequest>());
    }

    [TestMethod]
    public async Task CollectAsync_NoRulesFetchesAllAssets()
    {
        var client = Substitute.For<IImmichClient>();
        SearchAssetsRequest? capturedRequest = null;
        client.SearchAssetsAllPagesAsync(FakeImmich, Arg.Any<SearchAssetsRequest>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.Arg<SearchAssetsRequest>();
                return [new ImmichAsset { Id = "all-1", Type = "IMAGE" }];
            });

        var collector = new PlaylistAssetCollector(client);
        var profile = new Profile();

        var assets = await collector.CollectAsync(profile, FakeImmich);

        Assert.AreEqual(1, assets.Count);
        Assert.IsNotNull(capturedRequest);
        Assert.IsNull(capturedRequest.AlbumIds);
        Assert.IsNull(capturedRequest.PersonIds);
        Assert.IsNull(capturedRequest.TagIds);
    }

    [TestMethod]
    public async Task CollectAsync_RangeDateFilterSendsFullIsoDatetimes()
    {
        var client = Substitute.For<IImmichClient>();
        SearchAssetsRequest? capturedRequest = null;
        client.SearchAssetsAllPagesAsync(FakeImmich, Arg.Any<SearchAssetsRequest>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.Arg<SearchAssetsRequest>();
                return [new ImmichAsset { Id = "range-1", Type = "IMAGE" }];
            });

        var collector = new PlaylistAssetCollector(client);
        var profile = new Profile
        {
            DateFilter = new() { Type = "range", From = "2009-01-01", To = "2009-12-31" }
        };

        await collector.CollectAsync(profile, FakeImmich);

        Assert.IsNotNull(capturedRequest);
        Assert.AreEqual("2009-01-01T00:00:00.000Z", capturedRequest.TakenAfter);
        Assert.AreEqual("2009-12-31T23:59:59.999Z", capturedRequest.TakenBefore);
    }

    [TestMethod]
    public async Task CollectAsync_RollingDateFilterSendsFullIsoDatetime()
    {
        var client = Substitute.For<IImmichClient>();
        SearchAssetsRequest? capturedRequest = null;
        client.SearchAssetsAllPagesAsync(FakeImmich, Arg.Any<SearchAssetsRequest>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.Arg<SearchAssetsRequest>();
                return [new ImmichAsset { Id = "rolling-1", Type = "IMAGE" }];
            });

        var collector = new PlaylistAssetCollector(client);
        var profile = new Profile
        {
            DateFilter = new() { Type = "rolling", Amount = 8, Unit = "weeks" }
        };

        await collector.CollectAsync(profile, FakeImmich);

        Assert.IsNotNull(capturedRequest);
        Assert.IsNotNull(capturedRequest.TakenAfter);
        StringAssert.Matches(capturedRequest.TakenAfter, new System.Text.RegularExpressions.Regex(
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$"));
        Assert.IsNull(capturedRequest.TakenBefore);
    }

    [TestMethod]
    public async Task CollectAsync_DedupesDuplicateAssetsReturnedByRuleLeaf()
    {
        var client = Substitute.For<IImmichClient>();
        client.SearchAssetsAllPagesAsync(FakeImmich, Arg.Any<SearchAssetsRequest>())
            .Returns(
            [
                new ImmichAsset { Id = "dup-1", Type = "IMAGE" },
                new ImmichAsset { Id = "dup-1", Type = "IMAGE" },
                new ImmichAsset { Id = "unique-1", Type = "IMAGE" }
            ]);

        var collector = new PlaylistAssetCollector(client);
        var profile = new Profile
        {
            AssetFilter = new AssetFilterRule
            {
                Kind = AssetFilterRuleKind.Condition,
                Type = AssetFilterConditionType.Album,
                Id = "album-1",
                Label = "Album 1"
            }
        };

        var assets = await collector.CollectAsync(profile, FakeImmich);

        Assert.AreEqual(2, assets.Count);
        Assert.IsTrue(assets.ContainsKey("dup-1"));
        Assert.IsTrue(assets.ContainsKey("unique-1"));
        Assert.AreEqual("Album 1", assets["dup-1"].SourceLabel);
    }

    [TestMethod]
    public async Task CollectAsync_FiltersAssetsBelowMinimumFileSize()
    {
        var client = Substitute.For<IImmichClient>();
        client.SearchAssetsAllPagesAsync(FakeImmich, Arg.Any<SearchAssetsRequest>())
            .Returns(
            [
                new ImmichAsset
                {
                    Id = "small-1",
                    Type = "IMAGE",
                    ExifInfo = new ImmichExifInfo { FileSizeInByte = 350 * 1024L, ExifImageWidth = 1280, ExifImageHeight = 720 }
                },
                new ImmichAsset
                {
                    Id = "large-1",
                    Type = "IMAGE",
                    ExifInfo = new ImmichExifInfo { FileSizeInByte = 1800 * 1024L, ExifImageWidth = 3840, ExifImageHeight = 2160 }
                }
            ]);

        var collector = new PlaylistAssetCollector(client);
        var profile = new Profile
        {
            Quality = new QualitySettings { MinFileSizeKb = 1000 }
        };

        var assets = await collector.CollectAsync(profile, FakeImmich);

        Assert.AreEqual(1, assets.Count);
        Assert.IsFalse(assets.ContainsKey("small-1"));
        Assert.IsTrue(assets.ContainsKey("large-1"));
    }
}
