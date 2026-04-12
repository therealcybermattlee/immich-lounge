using System.Linq;
using ImmichLoungeCompanion.Models;
using ImmichLoungeCompanion.Playlist;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImmichLoungeCompanion.Tests.Playlist;

[TestClass]
public class PlaylistShuffleOrderTests
{
    [TestMethod]
    public void PreserveExistingOrder_KeepsExistingRelativeOrderForSharedAssets()
    {
        var existing = new[]
        {
            Entry("b"),
            Entry("a"),
            Entry("c")
        };
        var rebuilt = new[]
        {
            Entry("c"),
            Entry("a"),
            Entry("b")
        }.ToList();

        var result = PlaylistShuffleOrder.PreserveExistingOrder(existing, rebuilt);

        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, result.Select(entry => entry.Id).ToArray());
    }

    [TestMethod]
    public void PreserveExistingOrder_DropsRemovedAssetsAndAppendsNewOnes()
    {
        var existing = new[]
        {
            Entry("b"),
            Entry("a"),
            Entry("c")
        };
        var rebuilt = new[]
        {
            Entry("c"),
            Entry("d"),
            Entry("a")
        }.ToList();

        var result = PlaylistShuffleOrder.PreserveExistingOrder(existing, rebuilt);

        CollectionAssert.AreEqual(new[] { "a", "c", "d" }, result.Select(entry => entry.Id).ToArray());
    }

    private static PlaylistEntry Entry(string id) => new(id, "photo", null, null);
}
