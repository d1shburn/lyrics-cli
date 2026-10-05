namespace Lyrics;

public sealed record LyricsWord(
    TimeSpan Start,
    TimeSpan? End,
    string Text);