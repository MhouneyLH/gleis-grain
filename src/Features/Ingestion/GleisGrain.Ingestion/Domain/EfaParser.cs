using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using GleisGrain.Ingestion.Contracts;

namespace GleisGrain.Ingestion.Domain;

/// <summary>A parsed departure monitor answer. <see cref="RequestedAt"/> is the server clock at the time of the request.</summary>
internal sealed record EfaDepartureBoard(DateTimeOffset RequestedAt, IReadOnlyList<DepartureSnapshot> Departures);

/// <summary>
/// Reads the XML of the Linz AG EFA server. EFA writes times as local time without an offset,
/// so they are read as Europe/Vienna (the machine needs tzdata for this zone).
/// </summary>
internal static partial class EfaParser
{
    private const string CancelledDelay = "-9999";

    private static readonly TimeZoneInfo _vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    public static EfaDepartureBoard ParseDepartures(string xml)
    {
        var root = Load(xml);
        var requestedAt = ToViennaTime(ParseServerTime(root));

        var departures = root
            .Descendants("itdDeparture")
            .Select(ReadDeparture)
            .OfType<DepartureSnapshot>()
            .ToList();

        return new EfaDepartureBoard(requestedAt, departures);
    }

    /// <summary>Every EFA answer carries the current notices, so this works on any answer.</summary>
    public static IReadOnlyList<NoticeSnapshot> ParseNotices(string xml) =>
        Load(xml).Descendants("infoLink").Select(ReadNotice).OfType<NoticeSnapshot>().ToList();

    private static DepartureSnapshot? ReadDeparture(XElement departure)
    {
        var line = departure.Element("itdServingLine");
        var number = line?.Attribute("number")?.Value;
        var planned = ReadTime(departure.Element("itdDateTimeBaseTimetable")) ?? ReadTime(departure.Element("itdDateTime"));

        // A cancelled trip is marked with the delay -9999. It will not come, so the board leaves it out.
        if (line is null || number is null || planned is null || line.Element("itdNoTrain")?.Attribute("delay")?.Value == CancelledDelay)
        {
            return null;
        }

        var actual = ReadTime(departure.Element("itdRTDateTime")) ?? planned.Value;
        var delayMinutes = (int)Math.Round((actual - planned.Value).TotalMinutes);

        return new DepartureSnapshot(number, line.Attribute("direction")?.Value ?? string.Empty, planned.Value, delayMinutes, ReadPlatform(departure));
    }

    private static string? ReadPlatform(XElement departure)
    {
        var name = departure.Attribute("platformName")?.Value;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var number = departure.Attribute("platform")?.Value;
        return string.IsNullOrWhiteSpace(number) || number == "0" ? null : number;
    }

    private static NoticeSnapshot? ReadNotice(XElement link)
    {
        var id = link.Descendants("param").FirstOrDefault(p => p.Element("name")?.Value == "infoID")?.Element("value")?.Value;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var title = (link.Element("infoLinkText")?.Value ?? string.Empty).Trim();
        var text = HtmlToText(link.Element("infoText")?.Element("content")?.Value ?? string.Empty);

        return new NoticeSnapshot(id, title, text, ReadLines(title), []);
    }

    /// <summary>Titles name their lines up front: "L 26: Verlegung ..." or "L 191,192: Umleitung ...".</summary>
    private static string[] ReadLines(string title)
    {
        var match = LinesInTitle().Match(title);
        return match.Success
            ? [.. match.Groups["lines"].Value.Split([',', '/'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)]
            : [];
    }

    private static string HtmlToText(string html)
    {
        var text = HtmlBlockEnd().Replace(html, "\n");
        text = HtmlTag().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
        text = Blanks().Replace(text, " ");

        return string.Join('\n', text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
    }

    private static DateTimeOffset? ReadTime(XElement? dateTime)
    {
        var date = dateTime?.Element("itdDate");
        var time = dateTime?.Element("itdTime");
        if (date is null || time is null)
        {
            return null;
        }

        var local = new DateTime(
            Number(date, "year"),
            Number(date, "month"),
            Number(date, "day"),
            Number(time, "hour"),
            Number(time, "minute"),
            0,
            DateTimeKind.Unspecified);

        return ToViennaTime(local);
    }

    private static DateTime ParseServerTime(XElement root) =>
        DateTime.ParseExact(
            root.Attribute("now")?.Value ?? throw new InvalidDataException("The EFA answer has no 'now' attribute."),
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture);

    private static int Number(XElement element, string attribute) =>
        int.Parse(element.Attribute(attribute)?.Value ?? throw new InvalidDataException($"'{element.Name}' has no '{attribute}' attribute."), CultureInfo.InvariantCulture);

    private static DateTimeOffset ToViennaTime(DateTime local) => new(local, _vienna.GetUtcOffset(local));

    private static XElement Load(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var root = XDocument.Load(reader).Root;

        return root is { Name.LocalName: "itdRequest" } ? root : throw new InvalidDataException("This is not an EFA answer.");
    }

    [GeneratedRegex(@"^\s*(?:L|Linie|Linien)\s+(?<lines>[0-9A-Za-z]+(?:\s*[,/]\s*[0-9A-Za-z]+)*)\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinesInTitle();

    [GeneratedRegex(@"<(?:br|/p|/li|/div)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlBlockEnd();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"[^\S\n]+")]
    private static partial Regex Blanks();
}
