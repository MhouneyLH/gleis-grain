using GleisGrain.Stops.Catalogue;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Stops.Tests;

public class StopCatalogueTests
{
    private static readonly StopCatalogue _catalogue = StopCatalogue.Load();

    [Fact]
    public void All_HasTheCuratedTwentyFiveStops() =>
        Assert.Equal(25, _catalogue.All.Count);

    [Fact]
    public void All_HasUniqueIds() =>
        Assert.Equal(_catalogue.All.Count, _catalogue.All.Select(stop => stop.Id).Distinct().Count());

    [Fact]
    public void All_HasNames() =>
        Assert.All(_catalogue.All, stop => Assert.False(string.IsNullOrWhiteSpace(stop.Name)));

    [Fact]
    public void All_LiesInTheLinzArea() =>
        Assert.All(_catalogue.All, stop =>
        {
            Assert.InRange(stop.Latitude, 48.2, 48.4);
            Assert.InRange(stop.Longitude, 14.2, 14.45);
        });

    [Fact]
    public void TryGet_CuratedStop_ReturnsIt()
    {
        var first = _catalogue.All[0];

        Assert.True(_catalogue.TryGet(first.Id, out var found));
        Assert.Equal(first, found);
    }

    [Fact]
    public void TryGet_UnknownStop_ReturnsFalse() =>
        Assert.False(_catalogue.TryGet(StopId.From("nowhere"), out _));
}
