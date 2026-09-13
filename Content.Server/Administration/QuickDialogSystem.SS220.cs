using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration;

/// <summary>
/// This handles the server portion of quick dialogs, including opening them.
/// </summary>
public sealed partial class QuickDialogSystem : EntitySystem
{
    private void OpenDialogInternal(ICommonSession session, string title, string description, List<QuickDialogEntry> entries, QuickDialogButtonFlag buttons, Action<QuickDialogResponseEvent> okAction, Action cancelAction)
    {
        var did = GetDialogId();
        RaiseNetworkEvent(
            new QuickDialogDescOpenEvent(
                title,
                description,
                entries,
                did,
                buttons),
            session
        );

        _openDialogs.Add(did, (okAction, cancelAction));
        if (!_openDialogsByUser.ContainsKey(session.UserId))
            _openDialogsByUser.Add(session.UserId, new List<int>());

        _openDialogsByUser[session.UserId].Add(did);
    }

    private void OpenDialogInternalForTTSPrototypeId(ICommonSession session, string title, string description, EntityUid target, List<QuickDialogEntry> entries,
        QuickDialogButtonFlag buttons, Action<QuickDialogResponseEvent> okAction, Action cancelAction)
    {
        if (!TryGetNetEntity(target, out var netEntity))
            return;

        var did = GetDialogId();
        RaiseNetworkEvent(
            new QuickDialogTTSProtoIdOpenEvent(
                title,
                description,
                netEntity.Value,
                entries,
                did,
                buttons),
            session
        );

        _openDialogs.Add(did, (okAction, cancelAction));
        if (!_openDialogsByUser.ContainsKey(session.UserId))
            _openDialogsByUser.Add(session.UserId, new List<int>());

        _openDialogsByUser[session.UserId].Add(did);
    }
}
