using GleisGrain.Ingestion.Domain;

namespace GleisGrain.Ingestion.Tests;

public class EfaParserTests
{
    [Fact]
    public void ParseDepartures_RealtimeLaterThanPlanned_ReportsDelayInMinutes()
    {
        var xml = EfaXml.Board("2026-11-17T10:00:00", EfaXml.Departure("2026-11-17 10:05", realtime: "2026-11-17 10:07", number: "2", direction: "Universität"));

        var departure = Assert.Single(EfaParser.ParseDepartures(xml).Departures);

        Assert.Equal("2", departure.Line);
        Assert.Equal("Universität", departure.Direction);
        Assert.Equal(new DateTimeOffset(2026, 11, 17, 10, 5, 0, TimeSpan.FromHours(1)), departure.PlannedAt);
        Assert.Equal(2, departure.DelayMinutes);
    }

    [Fact]
    public void ParseDepartures_RealtimeEarlierThanPlanned_ReportsNegativeDelay()
    {
        var xml = EfaXml.Board("2026-11-17T10:00:00", EfaXml.Departure("2026-11-17 10:05", realtime: "2026-11-17 10:04"));

        Assert.Equal(-1, Assert.Single(EfaParser.ParseDepartures(xml).Departures).DelayMinutes);
    }

    [Fact]
    public void ParseDepartures_NoRealtime_HasNoDelay()
    {
        var xml = EfaXml.Board("2026-11-17T10:00:00", EfaXml.Departure("2026-11-17 10:05"));

        Assert.Equal(0, Assert.Single(EfaParser.ParseDepartures(xml).Departures).DelayMinutes);
    }

    [Fact]
    public void ParseDepartures_CancelledTrip_IsLeftOut()
    {
        var xml = EfaXml.Board(
            "2026-11-17T10:00:00",
            EfaXml.Departure("2026-11-17 10:05", number: "1", noTrainDelay: "-9999"),
            EfaXml.Departure("2026-11-17 10:10", number: "2", noTrainDelay: "0"));

        var departure = Assert.Single(EfaParser.ParseDepartures(xml).Departures);

        Assert.Equal("2", departure.Line);
    }

    [Theory]
    [InlineData("2026-01-15 12:00", 1)]
    [InlineData("2026-07-15 12:00", 2)]
    [InlineData("2026-03-29 01:30", 1)]
    [InlineData("2026-03-29 03:30", 2)]
    [InlineData("2026-10-25 01:30", 2)]
    [InlineData("2026-10-25 03:30", 1)]
    public void ParseDepartures_LocalTime_ReadsViennaOffsetOfThatDay(string local, int expectedOffsetHours)
    {
        var xml = EfaXml.Board("2026-11-17T10:00:00", EfaXml.Departure(local));

        var departure = Assert.Single(EfaParser.ParseDepartures(xml).Departures);

        Assert.Equal(TimeSpan.FromHours(expectedOffsetHours), departure.PlannedAt.Offset);
        Assert.Equal(DateTime.Parse(local, System.Globalization.CultureInfo.InvariantCulture), departure.PlannedAt.DateTime);
    }

    [Fact]
    public void ParseDepartures_RequestTime_IsTheServerClockInVienna()
    {
        var board = EfaParser.ParseDepartures(EfaXml.Board("2026-07-01T08:30:15"));

        Assert.Equal(new DateTimeOffset(2026, 7, 1, 8, 30, 15, TimeSpan.FromHours(2)), board.RequestedAt);
        Assert.Empty(board.Departures);
    }

    [Theory]
    [InlineData("A", "5", "A")]
    [InlineData("", "5", "5")]
    [InlineData("", "0", null)]
    [InlineData("", "", null)]
    public void ParseDepartures_Platform_PrefersNameThenNumberAndTreatsZeroAsUnknown(string platformName, string platform, string? expected)
    {
        var xml = EfaXml.Board("2026-11-17T10:00:00", EfaXml.Departure("2026-11-17 10:05", platformName: platformName, platform: platform));

        Assert.Equal(expected, Assert.Single(EfaParser.ParseDepartures(xml).Departures).Platform);
    }

    [Fact]
    public void ParseDepartures_NotAnEfaAnswer_Throws() =>
        Assert.Throws<InvalidDataException>(() => EfaParser.ParseDepartures("<html/>"));

    [Fact]
    public void ParseDepartures_DocumentTypeDeclaration_IsRejected()
    {
        const string xml = """<!DOCTYPE itdRequest [<!ENTITY x "y">]><itdRequest now="2026-11-17T10:00:00"/>""";

        Assert.ThrowsAny<System.Xml.XmlException>(() => EfaParser.ParseDepartures(xml));
    }

    [Fact]
    public void ParseNotices_Notice_StripsHtmlAndDecodesEntities()
    {
        var xml = EfaXml.Notices(("123_LL", "L 26: Verlegung Haltestelle Theater", "<p><strong>Ab Mai</strong>&nbsp;bis Ende</p><p>Die Hst. wird 90 m verlegt &amp; Bus f&uuml;hrt.<br />Danke</p>"));

        var notice = Assert.Single(EfaParser.ParseNotices(xml));

        Assert.Equal("123_LL", notice.Id);
        Assert.Equal("L 26: Verlegung Haltestelle Theater", notice.Title);
        Assert.Equal("Ab Mai bis Ende\nDie Hst. wird 90 m verlegt & Bus führt.\nDanke", notice.Text);
    }

    [Theory]
    [InlineData("L 26: Verlegung", new[] { "26" })]
    [InlineData("L 191,192: Umleitung", new[] { "191", "192" })]
    [InlineData("Linie 12: Umleitung", new[] { "12" })]
    [InlineData("Haltestelle Theater verlegt", new string[0])]
    [InlineData("Lange Nacht der Museen: Extrafahrten", new string[0])]
    public void ParseNotices_Title_ListsTheLinesItNames(string title, string[] expected)
    {
        var xml = EfaXml.Notices(("1", title, "<p>text</p>"));

        Assert.Equal(expected, Assert.Single(EfaParser.ParseNotices(xml)).Lines);
    }

    [Fact]
    public void ParseNotices_StopIds_AreEmptyBecauseTheXmlNamesNone()
    {
        var xml = EfaXml.Notices(("1", "L 1: x", "<p>text</p>"));

        Assert.Empty(Assert.Single(EfaParser.ParseNotices(xml)).StopIds);
    }

    [Fact]
    public void ParseNotices_NoNotices_IsEmpty() =>
        Assert.Empty(EfaParser.ParseNotices(EfaXml.Notices()));

    [Fact]
    public void ParseNotices_RecordedAnswer_ParsesEveryNoticeToText()
    {
        var notices = EfaParser.ParseNotices(EfaXml.ReadRecording("Replay/notices.xml"));

        Assert.NotEmpty(notices);
        Assert.All(notices, notice =>
        {
            Assert.False(string.IsNullOrWhiteSpace(notice.Id));
            Assert.False(string.IsNullOrWhiteSpace(notice.Title));
            Assert.False(string.IsNullOrWhiteSpace(notice.Text));
            Assert.DoesNotContain('<', notice.Text);
        });
    }
}
