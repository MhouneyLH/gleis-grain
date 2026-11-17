using System.Globalization;
using GleisGrain.Ingestion.Feeds;

namespace GleisGrain.Ingestion.Tests;

/// <summary>Builds small EFA answers by hand and reads the recorded ones.</summary>
internal static class EfaXml
{
    /// <summary>Local times are written like "2026-11-17 10:05".</summary>
    public static string Board(string now, params string[] departures) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <itdRequest version="10.6.34.3" now="{now}">
          <itdDepartureMonitorRequest><itdDepartureList>{string.Concat(departures)}</itdDepartureList></itdDepartureMonitorRequest>
        </itdRequest>
        """;

    public static string Departure(
        string planned,
        string? realtime = null,
        string number = "1",
        string direction = "Auwiesen",
        string platformName = "",
        string platform = "0",
        string? noTrainDelay = null) =>
        $"""
        <itdDeparture platform="{platform}" platformName="{platformName}">
          {Time("itdDateTime", planned)}
          {Time("itdDateTimeBaseTimetable", planned)}
          {(realtime is null ? string.Empty : Time("itdRTDateTime", realtime))}
          <itdServingLine number="{number}" direction="{direction}">
            <itdNoTrain name="Straßenbahn"{(noTrainDelay is null ? string.Empty : $" delay=\"{noTrainDelay}\"")}/>
          </itdServingLine>
        </itdDeparture>
        """;

    public static string Notices(params (string Id, string Title, string Html)[] notices) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <itdRequest version="10.6.34.3" now="2026-11-17T10:00:00">
          <itdInfoLinkList><itdBannerInfoList>{string.Concat(notices.Select(Notice))}</itdBannerInfoList></itdInfoLinkList>
        </itdRequest>
        """;

    public static string ReadRecording(string name)
    {
        using var stream = typeof(ReplayFeed).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"No recording {name}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static IEnumerable<string> RecordingNames() =>
        typeof(ReplayFeed).Assembly.GetManifestResourceNames().Where(n => n.StartsWith("Replay/", StringComparison.Ordinal));

    private static string Notice((string Id, string Title, string Html) notice) =>
        $"""
        <infoLink>
          <paramList>
            <param edit="0"><type>system</type><name>infoID</name><value>{notice.Id}</value></param>
            <param edit="0"><type>system</type><name>seqID</name><value>5</value></param>
          </paramList>
          <infoLinkText>{notice.Title}</infoLinkText>
          <infoText><content>{System.Security.SecurityElement.Escape(notice.Html)}</content></infoText>
        </infoLink>
        """;

    private static string Time(string element, string local)
    {
        var time = DateTime.ParseExact(local, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        return $"""<{element}><itdDate year="{time.Year}" month="{time.Month}" day="{time.Day}"/><itdTime hour="{time.Hour}" minute="{time.Minute}"/></{element}>""";
    }
}
