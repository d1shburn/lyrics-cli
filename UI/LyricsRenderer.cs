namespace Lyrics.UI;

public enum LyricsDisplayMode
{
    Line,
    Word
}

public sealed class LyricsRenderer
{
    private const int WordScale = 5;

    private readonly int _visibleLines;
    private readonly LyricsDisplayMode _mode;

    private int _lastIndex = -1;
    private string _lastWord = string.Empty;
    private double _gradientOffset;

    public LyricsRenderer(int visibleLines, LyricsDisplayMode mode)
    {
        if (visibleLines < 1)
            throw new ArgumentOutOfRangeException(nameof(visibleLines));

        _visibleLines =
            visibleLines % 2 == 0 ? visibleLines + 1 : visibleLines;

        _mode = mode;
    }

    public async Task RenderAsync(
        LyricsTrack lyrics,
        TimeSpan position,
        CancellationToken cancellationToken = default)
    {
        if (_mode == LyricsDisplayMode.Word)
        {
            await RenderWordAsync(lyrics, position, cancellationToken);
            return;
        }

        await RenderLineAsync(lyrics, position, cancellationToken);
    }

    public void Clear()
    {
        _lastIndex = -1;
        _lastWord = string.Empty;
        _gradientOffset = 0;

        Console.Write("\x1b[2J\x1b[H");
        Console.Write("\x1b[?25l");
    }

    public void Reset()
    {
        Console.Write("\x1b[0m");
        Console.Write("\x1b[?25h");
        Console.Clear();
    }

    private async Task RenderLineAsync(
        LyricsTrack lyrics,
        TimeSpan position,
        CancellationToken cancellationToken)
    {
        var currentIndex =
            lyrics.GetCurrentIndex(position);

        if (currentIndex < 0)
            return;

        if (currentIndex == _lastIndex)
        {
            _gradientOffset += 0.08;

            DrawLines(lyrics, currentIndex, 1);
            return;
        }

        var oldIndex = _lastIndex;
        _lastIndex = currentIndex;

        if (oldIndex < 0)
        {
            DrawLines(lyrics, currentIndex, 1);
            return;
        }

        await AnimateLinesAsync(lyrics, currentIndex, cancellationToken);
    }

    private async Task AnimateLinesAsync(
        LyricsTrack lyrics,
        int currentIndex,
        CancellationToken cancellationToken)
    {
        const int frames = 12;

        for (var frame = 0; frame < frames; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progress = (double)frame / (frames - 1);
            var eased = EaseInOut(progress);

            DrawLines(lyrics, currentIndex, eased);
            await Task.Delay(16, cancellationToken);
        }
    }

    private void DrawLines(
        LyricsTrack lyrics,
        int currentIndex,
        double animation)
    {
        var width = Math.Max(1, Console.WindowWidth);
        var height = Math.Max(1, Console.WindowHeight);
        var centerRow = height / 2;
        var half = _visibleLines / 2;

        Console.Write("\x1b[2J\x1b[H");

        for (var offset = -half; offset <= half; offset++)
        {
            var index = currentIndex + offset;

            if (index < 0 || index >= lyrics.Lines.Count)
                continue;

            var row = centerRow + offset;

            if (row < 0 || row >= height)
                continue;

            var text = lyrics.Lines[index].Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
                continue;

            text = TrimToWidth(text, width);

            var x = Math.Max(0, (width - text.Length) / 2);

            Console.SetCursorPosition(x, row);

            var distance = Math.Abs(offset);

            if (index == currentIndex)
            {
                DrawGradient(text, x, animation);
                continue;
            }

            if (distance == 1)
                Console.Write("\x1b[38;5;245m");
            else
                Console.Write("\x1b[38;5;238m");

            Console.Write(text);
            Console.Write("\x1b[0m");
        }

        Console.Write("\x1b[0m");
    }

    private async Task RenderWordAsync(
        LyricsTrack lyrics,
        TimeSpan position,
        CancellationToken cancellationToken)
    {
        var word = lyrics.GetCurrentWord(position);

        if (word is null)
            return;

        var text = word.Text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (text == _lastWord)
        {
            DrawWord(text);
            return;
        }

        _lastWord = text;

        await AnimateWordAsync(text, cancellationToken);
    }

    private async Task AnimateWordAsync(
        string word,
        CancellationToken cancellationToken)
    {
        const int frames = 14;

        for (var frame = 0; frame < frames; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progress = (double)frame / (frames - 1);
            var eased = EaseOut(progress);

            Console.Write("\x1b[2J\x1b[H");

            var width = Math.Max(1, Console.WindowWidth);
            var height = Math.Max(1, Console.WindowHeight);
            var scaledWidth = word.Length * WordScale;

            var x = Math.Max(0, (width - scaledWidth) / 2);

            var centerY = height / 2;
            var offset = (int)Math.Round((1 - eased) * 4);

            var y = Math.Max(0, centerY - 1 + offset);

            Console.SetCursorPosition(x, y);

            Console.Write("\x1b[1m");

            DrawScaledGradient(word, eased);

            Console.Write("\x1b[22m");
            Console.Write("\x1b[0m");

            await Task.Delay(16, cancellationToken);
        }
    }

    private void DrawWord(string word)
    {
        var width = Math.Max(1, Console.WindowWidth);
        var height = Math.Max(1, Console.WindowHeight);
        var scaledWidth = word.Length * WordScale;

        var x = Math.Max(0, (width - scaledWidth) / 2);
        var y = Math.Max(0, height / 2 - 1);

        Console.Write("\x1b[2J\x1b[H");

        Console.SetCursorPosition(x, y);

        Console.Write("\x1b[1m");

        DrawScaledGradient(word, 1);

        Console.Write("\x1b[22m");
        Console.Write("\x1b[0m");
    }

    private void DrawScaledGradient(string text, double intensity)
    {
        const double speed = 0.08;

        for (var i = 0; i < text.Length; i++)
        {
            var wave = i * 0.12 + _gradientOffset;
            var phase = (Math.Sin(wave) + 1) / 2;

            var r = (int)(150 + phase * 105);
            var g = (int)(80 + phase * 100);
            var b = (int)(220 + phase * 35);

            r = Math.Clamp((int)(r * intensity), 0, 255);
            g = Math.Clamp((int)(g * intensity), 0, 255);
            b = Math.Clamp((int)(b * intensity), 0, 255);

            Console.Write($"\x1b[38;2;{r};{g};{b}m");

            Console.Write($"\x1b]66;s={WordScale};{text[i]}\a");
        }

        Console.Write("\x1b[0m");

        _gradientOffset += speed;
    }

    private void DrawGradient(string text, int startX, double intensity)
    {
        const double speed = 0.08;

        for (var i = 0; i < text.Length; i++)
        {
            var wave = (i + startX) * 0.12 + _gradientOffset;
            var phase = (Math.Sin(wave) + 1) / 2;

            var r = (int)(150 + phase * 105);
            var g = (int)(80 + phase * 100);
            var b = (int)(220 + phase * 35);

            r = Math.Clamp((int)(r * intensity), 0, 255);
            g = Math.Clamp((int)(g * intensity), 0, 255);
            b = Math.Clamp((int)(b * intensity), 0, 255);

            Console.Write($"\x1b[38;2;{r};{g};{b}m");

            Console.Write(text[i]);
        }

        Console.Write("\x1b[0m");

        _gradientOffset += speed;
    }

    private static double EaseInOut(double value) =>
        value < 0.5
            ? 2 * value * value
            : 1 -
                Math.Pow(
                    -2 * value + 2,
                    2) / 2;

    private static double EaseOut(double value) =>
        1 - Math.Pow(1 - value, 3);

    private static string TrimToWidth(string text, int width)
    {
        if (text.Length <= width)
            return text;

        if (width <= 3)
            return text[..width];

        return text[..(width - 3)] + "...";
    }
}