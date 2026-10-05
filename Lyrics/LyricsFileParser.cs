using YamlDotNet.Serialization;

namespace Lyrics;

public sealed class LyricsFileParser
{
    private readonly IDeserializer _deserializer;

    public LyricsFileParser() =>
        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

    public LyricsTrack Parse(string yaml)
    {
        var document = _deserializer.Deserialize<Document>(yaml);

        if (document.Lines is null)
            return new LyricsTrack([]);

        var lines = document.Lines
            .Select(line =>
            {
                var words = line.Words?
                    .Select(word => new LyricsWord(
                        TimeSpan.FromMilliseconds(word.StartMs),
                        word.EndMs.HasValue
                            ? TimeSpan.FromMilliseconds(word.EndMs.Value)
                            : null,
                        word.Text))
                    .ToArray();

                return new LyricsLine(
                    TimeSpan.FromMilliseconds(line.StartMs),
                    line.Text,
                    words);
            })
            .ToArray();

        return new LyricsTrack(lines);
    }

    private sealed class Document
    {
        public List<Line>? Lines { get; set; }
    }

    private sealed class Line
    {
        public string Text { get; set; } = string.Empty;

        public long StartMs { get; set; }

        public long? EndMs { get; set; }

        public List<Word>? Words { get; set; }
    }

    private sealed class Word
    {
        public string Text { get; set; } = string.Empty;

        public long StartMs { get; set; }

        public long? EndMs { get; set; }
    }
}