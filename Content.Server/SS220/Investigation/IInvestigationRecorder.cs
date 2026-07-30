// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.Database;

namespace Content.Server.SS220.Investigation;

/// <summary>
///     Resolves the grid-local position of an arbitrary entity on demand.
/// </summary>
/// <remarks>
///     Implemented by <see cref="InvestigationRecorderSystem"/> and handed to the recorder at startup. It exists
///     because the recorder is an IoC manager with no way to walk transforms, but has to place speech from
///     entities that were never player-controlled and so have no sampled position.
/// </remarks>
public interface IInvestigationPositionSource
{
    /// <param name="grid">Grid the entity is on, or null when it is in space or parented to the map.</param>
    /// <param name="local">
    ///     Position in the grid's local frame, or the world frame when <paramref name="grid"/> is null.
    /// </param>
    /// <param name="container">The container the entity is inside, if any.</param>
    /// <returns>False if the entity no longer exists or has no transform.</returns>
    bool TryGetPosition(EntityUid uid, out EntityUid? grid, out Vector2 local, out EntityUid? container);
}

/// <summary>
///     Records a lightweight, per-round "light replay" bundle intended for out-of-game investigation tooling.
/// </summary>
/// <remarks>
///     This is deliberately not a replacement for engine replays. It captures only what an investigator needs:
///     where tracked characters were, what the station looked like, what people were carrying, and the admin log
///     stream pre-joined to positions. Because it stores orders of magnitude less data than a replay it does not
///     need the size cap that stops replay recording partway through long rounds
///     (see <c>replay.max_compressed_size</c>).
/// </remarks>
public interface IInvestigationRecorder
{
    /// <summary>
    ///     Whether a round is currently being recorded. Callers on hot paths should check this before doing any work.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    ///     Called for every admin log as it is created, before it is queued for the database.
    /// </summary>
    /// <param name="type">The log type.</param>
    /// <param name="impact">The log impact.</param>
    /// <param name="message">The fully formatted human-readable log message.</param>
    /// <param name="values">
    ///     The raw interpolation values captured by <see cref="Content.Shared.Administration.Logs.LogStringHandler"/>.
    ///     Entity holes are stored here as live <see cref="Robust.Shared.GameObjects.EntityStringRepresentation"/>
    ///     structs, which is what lets us resolve positions without re-parsing the serialized JSON.
    /// </param>
    void OnAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values);

    /// <summary>
    ///     Called for every in-character or out-of-character message, with the text as it was actually typed.
    /// </summary>
    /// <remarks>
    ///     This exists because chat text is not recoverable from the admin log stream. Bare interpolation holes are
    ///     dropped by <see cref="Content.Shared.Administration.Logs.LogStringHandler.AppendFormatted(string?)"/>,
    ///     which does not call AddFormat, so the message text survives only inside the formatted string. Readers need
    ///     speaker and text as separate fields to draw speech bubbles, and regex-recovering them from a localized,
    ///     per-channel message format would be fragile.
    /// </remarks>
    /// <param name="source">The speaking entity, or null for channels with no in-world speaker (OOC).</param>
    /// <param name="channel">Channel label: Say, Whisper, Radio, Emote, OOC, LOOC.</param>
    /// <param name="text">
    ///     The original, untransformed message. Not language-obfuscated, not accent-transformed, and with its
    ///     <c>%key</c> language prefixes left in, so a reader can split it back into per-language runs.
    /// </param>
    /// <param name="speakerName">Displayed name at the time, which may be a disguised identity.</param>
    /// <param name="defaultLanguage">
    ///     The speaker's selected language: what the unprefixed part of the message was spoken in.
    /// </param>
    /// <param name="languages">
    ///     Language prototype ids the message was actually spoken in, in the order they appear in the sentence.
    ///     Usually just the default one; more when the speaker switched language mid-message with a key prefix.
    ///     Null or empty for channels that carry no language at all (Emote, LOOC, OOC).
    /// </param>
    /// <param name="radioChannel">
    ///     Radio channel prototype id (<c>Security</c>, <c>Common</c>, …) for <c>Radio</c> messages, else null.
    ///     Which net a message went out on is half the meaning of a radio line, and it is not recoverable from the
    ///     text.
    /// </param>
    void OnChat(
        EntityUid? source,
        string channel,
        string text,
        string? speakerName,
        string? defaultLanguage = null,
        IReadOnlyList<string>? languages = null,
        string? radioChannel = null);
}
