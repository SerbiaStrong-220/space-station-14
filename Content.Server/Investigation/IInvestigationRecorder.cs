using Content.Shared.Database;

namespace Content.Server.Investigation;

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
    /// <param name="text">The original, untransformed message. Not language-obfuscated, not accent-transformed.</param>
    /// <param name="speakerName">Displayed name at the time, which may be a disguised identity.</param>
    void OnChat(EntityUid? source, string channel, string text, string? speakerName);
}
