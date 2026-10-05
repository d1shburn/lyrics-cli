using Lyrics;
using Lyrics.Mpris;
using Lyrics.UI;

var mode = LyricsDisplayMode.Line;
var visibleLines = 5;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--mode"
            when i + 1 < args.Length:

            mode = args[++i].ToLowerInvariant() switch
            {
                "word" => LyricsDisplayMode.Word,
                "line" => LyricsDisplayMode.Line,
                _ => LyricsDisplayMode.Line
            };

            break;

        case "--lines"
            when i + 1 < args.Length && int.TryParse(args[++i], out var lines):

            visibleLines = Math.Max(1, lines);
            break;
    }
}

var player = new MprisPlayer();

using var httpClient = new HttpClient();

var lyricsProvider = new LyricsProvider(httpClient);
var renderer = new LyricsRenderer(visibleLines, mode);

using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

renderer.Clear();

LyricsTrack? currentLyrics = null;
string? currentTrackId = null;

try
{
    while (cancellationTokenSource.IsCancellationRequested == false)
    {
        var track =
            await player.GetCurrentTrackAsync();

        if (track is null ||
            string.IsNullOrWhiteSpace(track.Artist) ||
            string.IsNullOrWhiteSpace(track.Title))
        {
            currentLyrics = null;
            currentTrackId = null;

            renderer.Clear();

            await Task.Delay(500, cancellationTokenSource.Token);
            continue;
        }

        var trackId = $"{track.Artist}\0{track.Title}";

        if (trackId != currentTrackId)
        {
            currentTrackId = trackId;

            var result =
                await lyricsProvider.GetAsync(
                    track.Artist,
                    track.Title,
                    track.Length,
                    cancellationTokenSource.Token);

            currentLyrics = result;

            renderer.Clear();
        }

        if (currentLyrics is not null)
        {
            await renderer.RenderAsync(
                currentLyrics,
                track.Position,
                cancellationTokenSource.Token);
        }

        await Task.Delay(50, cancellationTokenSource.Token);
    }
}
catch (OperationCanceledException)
{
    
}
finally
{
    renderer.Reset();
}