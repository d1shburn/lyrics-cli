using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Lyrics;

public sealed class LyricsProvider
{
    private static readonly Uri LrcMuxEndpoint =
        new("https://api.lrcmux.dev/get");

    private static readonly Uri LrcLibEndpoint =
        new("https://lrclib.net/api/get");

    private readonly HttpClient _httpClient;

    public LyricsProvider(HttpClient httpClient) =>
        _httpClient = httpClient;

    public async Task<LyricsTrack?> GetAsync(
        string? artist,
        string? title,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var lrcMuxResult = await TryGetFromLrcMuxAsync(
            artist,
            title,
            duration,
            cancellationToken);

        if (lrcMuxResult is not null)
            return lrcMuxResult;

        return await TryGetFromLrcLibAsync(
            artist,
            title,
            cancellationToken);
    }

    private async Task<LyricsTrack?> TryGetFromLrcMuxAsync(
        string? artist,
        string title,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"{LrcMuxEndpoint}" +
                $"?artist={Uri.EscapeDataString(artist ?? string.Empty)}" +
                $"&title={Uri.EscapeDataString(title)}" +
                $"&duration={duration.TotalSeconds.ToString(
                    "F0",
                    CultureInfo.InvariantCulture)}";

            using var response = await _httpClient.GetAsync(
                url,
                cancellationToken);

            if (response.IsSuccessStatusCode == false)
                return null;

            var result =
                await response.Content.ReadFromJsonAsync<LrcMuxResponse>(
                    cancellationToken: cancellationToken);

            Console.WriteLine(result?.Lines?.Count ?? 0);

            if (result?.Meta?.Instrumental is true)
                return null;

            if (result?.Lines is not { Count: > 0 })
                return null;

            var lines = result.Lines
                .Where(line =>
                    line is not null &&
                    line.Start.HasValue &&
                    string.IsNullOrWhiteSpace(line.Text) == false)
                .Select(line =>
                {
                    var words = line!.Words?
                        .Where(word =>
                            word is not null &&
                            word.Start >= 0 &&
                            string.IsNullOrWhiteSpace(word.Text) == false)
                        .Select(word =>
                            new LyricsWord(
                                TimeSpan.FromMilliseconds(
                                    Math.Max(0, word!.Start)),
                                word.End.HasValue
                                    ? TimeSpan.FromMilliseconds(
                                        Math.Max(0, word.End.Value))
                                    : null,
                                word.Text!.Trim()))
                        .OrderBy(word => word.Start)
                        .ToArray();

                    return new LyricsLine(
                        TimeSpan.FromMilliseconds(
                            Math.Max(0, line.Start!.Value)),
                        line.Text!.Trim(),
                        words is { Length: > 0 }
                            ? words
                            : null);
                })
                .OrderBy(line => line.Timestamp)
                .ToArray();

            if (lines.Length == 0)
                return null;

            return new LyricsTrack(lines);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private async Task<LyricsTrack?> TryGetFromLrcLibAsync(
        string? artist,
        string title,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"{LrcLibEndpoint}" +
                $"?artist_name={Uri.EscapeDataString(artist ?? string.Empty)}" +
                $"&track_name={Uri.EscapeDataString(title)}";

            using var response = await _httpClient.GetAsync(
                url,
                cancellationToken);

            if (response.IsSuccessStatusCode == false)
                return null;

            var result =
                await response.Content.ReadFromJsonAsync<LrcLibResponse>(
                    cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(result?.SyncedLyrics))
                return null;

            var lines = ParseLrc(result.SyncedLyrics);

            if (lines.Count == 0)
                return null;

            return new LyricsTrack(lines);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<LyricsLine> ParseLrc(string lyrics)
    {
        var result = new List<LyricsLine>();

        foreach (var rawLine in lyrics.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.Length < 10)
                continue;

            var closeBracket = line.IndexOf(']');

            if (closeBracket <= 0)
                continue;

            var timestamp = line[1..closeBracket];

            if (TimeSpan.TryParseExact(
                    timestamp,
                    @"mm\:ss\.ff",
                    CultureInfo.InvariantCulture,
                    out var time) == false)
            {
                continue;
            }

            var text = line[(closeBracket + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(text))
                continue;

            result.Add(new LyricsLine(time, text));
        }

        return result
            .OrderBy(line => line.Timestamp)
            .ToArray();
    }

    private sealed class LrcMuxResponse
    {
        [JsonPropertyName("meta")]
        public LrcMuxMeta? Meta { get; set; }

        [JsonPropertyName("lines")]
        public List<LrcMuxLine?>? Lines { get; set; }
    }

    private sealed class LrcMuxMeta
    {
        [JsonPropertyName("instrumental")]
        public bool Instrumental { get; set; }
    }

    private sealed class LrcMuxLine
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("start")]
        public long? Start { get; set; }

        [JsonPropertyName("words")]
        public List<LrcMuxWord?>? Words { get; set; }
    }

    private sealed class LrcMuxWord
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("start")]
        public long Start { get; set; }

        [JsonPropertyName("end")]
        public long? End { get; set; }
    }

    private sealed class LrcLibResponse
    {
        [JsonPropertyName("syncedLyrics")]
        public string? SyncedLyrics { get; set; }
    }
}