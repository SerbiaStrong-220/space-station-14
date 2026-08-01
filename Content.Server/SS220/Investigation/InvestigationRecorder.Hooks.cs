// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Text.Json;
using System.Globalization;
using Content.Shared.Database;
using Robust.Shared.Map;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorder
{
    /// <summary>Swallows anything a hook throws: an escape would take down chat or admin logging.</summary>
    private void Guarded(Action write)
    {
        try
        {
            write();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Investigation recording threw and has been stopped for this round: {e}");

            try
            {
                StopRound(null);
            }
            catch (Exception stopError)
            {
                _sawmill.Error($"Investigation recording also failed to stop cleanly: {stopError}");
                _session = null;
            }
        }
    }

    public void OnChat(
        EntityUid? source,
        string channel,
        string text,
        string? speakerName,
        string? defaultLanguage = null,
        IReadOnlyList<string>? languages = null,
        string? radioChannel = null)
    {
        if (_session is null || string.IsNullOrWhiteSpace(text))
            return;

        Guarded(() => WriteChat(source, channel, text, speakerName, defaultLanguage, languages, radioChannel));
    }

    private void WriteChat(
        EntityUid? source,
        string channel,
        string text,
        string? speakerName,
        string? defaultLanguage,
        IReadOnlyList<string>? languages,
        string? radioChannel)
    {
        if (_session is not { } session)
            return;

        SampledPosition position = default;
        var located = false;

        if (source is { } speaker)
        {
            located = _positions.TryGetValue(speaker, out position);

            if (!located
                && _positionSource is { } positionSource
                && positionSource.TryGetPosition(speaker, out var resolved))
            {
                position = resolved;
                located = true;
            }
        }

        session.Chat.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            e = source?.Id,
            ch = channel,
            name = speakerName,
            msg = text,
            lang = defaultLanguage ?? (languages is { Count: > 0 } ? languages[0] : null),
            langs = languages is { Count: > 1 } ? languages : null,
            rc = radioChannel,
            g = located ? position.Grid?.Id : null,
            x = located ? Math.Round(position.Local.X, 2) : (double?) null,
            y = located ? Math.Round(position.Local.Y, 2) : (double?) null,
            c = located ? position.Container?.Id : null,
        }, JsonOptions));
        session.RowCount++;
    }

    public void OnAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values)
    {
        if (_session is null)
            return;

        Guarded(() => WriteAdminLog(type, impact, message, values));
    }

    private void WriteAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values)
    {
        if (_session is not { } session)
            return;

        List<object>? entities = null;

        foreach (var (key, value) in values)
        {
            switch (value)
            {
                case EntityStringRepresentation entity:
                {
                    entities ??= new List<object>();

                    var known = _positions.TryGetValue(entity.Uid, out var position);

                    entities.Add(new
                    {
                        role = key,
                        e = entity.Uid.Id,
                        name = entity.Name,
                        prototype = entity.Prototype,
                        player = entity.Session?.UserId.UserId.ToString(),
                        g = known ? position.Grid?.Id : null,
                        x = known ? Math.Round(position.Local.X, 2) : (double?) null,
                        y = known ? Math.Round(position.Local.Y, 2) : (double?) null,
                        c = known ? position.Container?.Id : null,
                    });
                    break;
                }
                // Some call sites pass coordinates directly; those beat the sampled cache.
                case EntityCoordinates coordinates:
                {
                    entities ??= new List<object>();
                    entities.Add(new
                    {
                        role = key,
                        parent = coordinates.EntityId.Id,
                        x = Math.Round(coordinates.X, 2),
                        y = Math.Round(coordinates.Y, 2),
                    });
                    break;
                }
            }
        }

        session.Events.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            type = type.ToString(),
            impact = impact.ToString(),
            msg = message,
            entities,
        }, JsonOptions));
        session.RowCount++;
    }
}
