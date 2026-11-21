// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Database;

namespace Content.Server.SS220.Investigation;

public interface IInvestigationPositionSource
{
    bool TryGetPosition(EntityUid uid, out SampledPosition position);

    /// <summary>Writes every sample the dead-reckoning filter is holding back, before a session closes.</summary>
    void FlushPendingPositions();
}

public interface IInvestigationRecorder
{
    bool IsRecording { get; }

    void OnAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values);

    void OnChat(
        EntityUid? source,
        string channel,
        string text,
        string? speakerName,
        string? defaultLanguage = null,
        IReadOnlyList<string>? languages = null,
        string? radioChannel = null);

    void OnAhelp(
        EntityUid? source,
        Guid thread,
        string senderName,
        string text,
        bool senderIsAdmin,
        bool adminOnly);
}
