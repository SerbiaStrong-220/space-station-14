// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.Database;

namespace Content.Server.SS220.Investigation;

/// <summary>Resolves the grid-local position of an entity that has no sampled position.</summary>
public interface IInvestigationPositionSource
{
    /// <param name="grid">Grid the entity is on, or null when it is in space or parented to the map.</param>
    /// <returns>False if the entity no longer exists or has no transform.</returns>
    bool TryGetPosition(EntityUid uid, out EntityUid? grid, out Vector2 local, out EntityUid? container);
}

/// <summary>Records a lightweight, per-round bundle for out-of-game investigation tooling.</summary>
public interface IInvestigationRecorder
{
    bool IsRecording { get; }

    /// <param name="values">
    ///     Raw interpolation values. Entity holes are live <see cref="EntityStringRepresentation"/> structs, which
    ///     is what lets positions be resolved without re-parsing the serialized JSON.
    /// </param>
    void OnAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values);

    /// <param name="source">The speaking entity, or null for channels with no in-world speaker.</param>
    /// <param name="text">Untransformed, with its <c>%key</c> language prefixes left in.</param>
    /// <param name="speakerName">Displayed name at the time, which may be a disguised identity.</param>
    void OnChat(
        EntityUid? source,
        string channel,
        string text,
        string? speakerName,
        string? defaultLanguage = null,
        IReadOnlyList<string>? languages = null,
        string? radioChannel = null);
}
