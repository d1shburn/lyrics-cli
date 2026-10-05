namespace Lyrics;

public sealed record LyricsLine(
    TimeSpan Timestamp,
    string Text,
    IReadOnlyList<LyricsWord>? Words = null);