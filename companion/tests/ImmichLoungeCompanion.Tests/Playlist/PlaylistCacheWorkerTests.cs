using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImmichLoungeCompanion.Models;
using ImmichLoungeCompanion.Playlist;
using ImmichLoungeCompanion.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace ImmichLoungeCompanion.Tests.Playlist;

[TestClass]
public class PlaylistCacheWorkerTests
{
    private IPlaylistBuilder _builder = null!;
    private IPlaylistCache _cache = null!;
    private IProfileRepository _profiles = null!;
    private PlaylistCacheWorker _worker = null!;
    private PlaylistCacheEntry? _stored;

    [TestInitialize]
    public void Setup()
    {
        _builder = Substitute.For<IPlaylistBuilder>();
        _cache = Substitute.For<IPlaylistCache>();
        _profiles = Substitute.For<IProfileRepository>();
        _worker = new PlaylistCacheWorker(_builder, _cache, _profiles, NullLogger<PlaylistCacheWorker>.Instance);

        _cache.TryStartBuilding("p1").Returns(true);
        _cache.When(c => c.Set("p1", Arg.Any<PlaylistCacheEntry>()))
              .Do(call => _stored = call.Arg<PlaylistCacheEntry>());
        _profiles.GetAsync("p1").Returns(new Profile
        {
            Id = "p1",
            Slideshow = new() { Shuffle = true }
        });
    }

    [TestMethod]
    public async Task Rebuild_UnchangedContent_KeepsExistingOrderAndVersion()
    {
        _cache.Get("p1").Returns(new PlaylistCacheEntry(Entries("a", "b", "c"), System.DateTimeOffset.UtcNow, "v1"));
        _builder.BuildAsync(Arg.Any<Profile>(), Arg.Any<CancellationToken>())
                .Returns(Entries("c", "a", "b"));

        await _worker.RebuildAsync("p1");

        Assert.IsNotNull(_stored);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, _stored.Assets.Select(e => e.Id).ToArray());
        Assert.AreEqual("v1", _stored.PlaylistVersion);
    }

    [TestMethod]
    public async Task Rebuild_ChangedContent_KeepsFreshShuffleAndNewVersion()
    {
        _cache.Get("p1").Returns(new PlaylistCacheEntry(Entries("a", "b", "c"), System.DateTimeOffset.UtcNow, "v1"));
        _builder.BuildAsync(Arg.Any<Profile>(), Arg.Any<CancellationToken>())
                .Returns(Entries("d", "c", "a", "b"));

        await _worker.RebuildAsync("p1");

        // The builder's fresh shuffle must be kept as-is: preserve-and-append would
        // pin the old head ("a","b","c",...) forever, replaying the same window
        // every time a version change resets clients to offset 0.
        Assert.IsNotNull(_stored);
        CollectionAssert.AreEqual(new[] { "d", "c", "a", "b" }, _stored.Assets.Select(e => e.Id).ToArray());
        Assert.AreNotEqual("v1", _stored.PlaylistVersion);
    }

    private static List<PlaylistEntry> Entries(params string[] ids)
        => ids.Select(id => new PlaylistEntry(id, "photo", null, null)).ToList();
}
