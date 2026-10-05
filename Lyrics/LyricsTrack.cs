namespace Lyrics;

public sealed class LyricsTrack
{
    private readonly IReadOnlyList<LyricsLine> _lines;

    public IReadOnlyList<LyricsLine> Lines => _lines;

    public bool HasWordSync =>
        _lines.Any(line => line.Words is { Count: > 0 });

    public LyricsTrack(
        IEnumerable<LyricsLine> lines)
    {
        _lines = lines
            .OrderBy(line => line.Timestamp)
            .ToArray();
    }

    public int GetCurrentIndex(TimeSpan position)
    {
        var low = 0;
        var high = _lines.Count - 1;
        var result = -1;

        while (low <= high)
        {
            var middle =
                low + (high - low) / 2;

            if (_lines[middle].Timestamp <= position)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return result;
    }

    public LyricsWord? GetCurrentWord(TimeSpan position)
    {
        var lineIndex =
            GetCurrentIndex(position);

        if (lineIndex < 0)
            return null;

        var words = _lines[lineIndex].Words;

        if (words is not { Count: > 0 })
            return null;

        var low = 0;
        var high = words.Count - 1;
        var result = -1;

        while (low <= high)
        {
            var middle =
                low + (high - low) / 2;

            if (words[middle].Start <= position)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return result >= 0 ? words[result] : null;
    }
}