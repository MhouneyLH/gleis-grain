using System.Text.RegularExpressions;

namespace GleisGrain.Common.Tests;

public partial class SiloNamesTests
{
    [GeneratedRegex("^v[0-9]+-[0-9a-f]{5}$")]
    private static partial Regex NameFormat();

    [Theory]
    [InlineData(1, 0x3f9a1, "v1-3f9a1")]
    [InlineData(2, 0xb72c0, "v2-b72c0")]
    [InlineData(2, 0xf, "v2-0000f")]
    public void Create_VersionAndRandom_BuildsPrefixAndFiveHexChars(int version, int random, string expected) =>
        Assert.Equal(expected, SiloNames.Create(version, random));

    [Fact]
    public void Create_RandomSuffix_MatchesTheNameContract()
    {
        for (var i = 0; i < 50; i++)
        {
            Assert.Matches(NameFormat(), SiloNames.Create(2));
        }
    }

    [Fact]
    public void Create_VersionBelowOne_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => SiloNames.Create(0));

    [Theory]
    [InlineData("v1-3f9a1", 1)]
    [InlineData("v2-b72c0", 2)]
    [InlineData("v12-abcde", 12)]
    public void ParseVersion_ValidName_ReturnsVersion(string name, int expected) =>
        Assert.Equal(expected, SiloNames.ParseVersion(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Silo_3f9a1")]
    [InlineData("v-3f9a1")]
    [InlineData("vx-3f9a1")]
    [InlineData("v2")]
    public void ParseVersion_OtherName_ReturnsNull(string? name) =>
        Assert.Null(SiloNames.ParseVersion(name));

    [Fact]
    public void ParseVersion_OfCreatedName_RoundTrips() =>
        Assert.Equal(2, SiloNames.ParseVersion(SiloNames.Create(2)));
}
