using Lyrics.DBus;
using Tmds.DBus.Protocol;

namespace Lyrics.Mpris;

public sealed class MprisPlayer
{
    private const string ObjectPath = "/org/mpris/MediaPlayer2";
    private const string MediaPlayerPrefix = "org.mpris.MediaPlayer2.";

    private readonly DBusConnection _connection;

    public MprisPlayer() =>
        _connection = new DBusConnection(
            DBusAddress.Session
            ?? throw new InvalidOperationException(
                "D-Bus session is unavailable."));

    public async Task<TrackState?> GetCurrentTrackAsync()
    {
        await _connection.ConnectAsync();

        var players = await GetPlayersAsync();

        foreach (var serviceName in players)
        {
            try
            {
                var service = new DBusService(_connection, serviceName);
                var player = service.CreatePlayer(ObjectPath);
                var status = await player.GetPlaybackStatusAsync();

                if (status is not "Playing" and not "Paused")
                    continue;

                var metadata = await player.GetMetadataAsync();
                var position = await player.GetPositionAsync();

                var title = GetString(metadata, "xesam:title");

                if (string.IsNullOrWhiteSpace(title))
                    continue;

                var artist = GetArtist(metadata);

                var length = GetInt64(metadata, "mpris:length");

                return new TrackState(
                    serviceName,
                    title,
                    artist,
                    TimeSpan.FromMicroseconds(position),
                    TimeSpan.FromMicroseconds(length),
                    status);
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<string>> GetPlayersAsync()
    {
        var services = await _connection.ListServicesAsync();

        return services
            .Where(service =>
                service.StartsWith(
                    MediaPlayerPrefix,
                    StringComparison.Ordinal))
            .ToArray();
    }

    private static string? GetString(
        IReadOnlyDictionary<string, VariantValue> metadata,
        string key)
    {
        if (metadata.TryGetValue(key, out var value) == false)
            return null;

        return value.GetString();
    }

    private static string? GetArtist(
        IReadOnlyDictionary<string, VariantValue> metadata)
    {
        if (metadata.TryGetValue("xesam:artist", out var value) == false)
            return null;

        var artists = value.GetArray<string>();
        return artists.FirstOrDefault();
    }

    private static long GetInt64(
        IReadOnlyDictionary<string, VariantValue> metadata,
        string key)
    {
        if (metadata.TryGetValue(key, out var value) == false)
            return 0;

        return value.GetInt64();
    }
}

public sealed record TrackState(
    string Player,
    string? Title,
    string? Artist,
    TimeSpan Position,
    TimeSpan Length,
    string PlaybackStatus);