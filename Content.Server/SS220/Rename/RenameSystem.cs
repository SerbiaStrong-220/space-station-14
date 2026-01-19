using Content.Server.Administration;
using Content.Shared.SS220.Rename;
using Robust.Shared.Player;

namespace Content.Server.SS220.Rename;

public sealed class RenameSystem : SharedRenameSystem
{
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;

    protected override void Act(EntityUid user, EntityUid target)
    {
        base.Act(user, target);

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("admin-verbs-dialog-rename-and-redescribe-title"), Loc.GetString("admin-verbs-dialog-rename-name"), Loc.GetString("admin-verbs-dialog-redescribe-description"),
            (string newName, LongString newDescription) =>
            {
                var meta = MetaData(target);
                _metaSystem.SetEntityName(target, newName, meta);
                _metaSystem.SetEntityDescription(target, newDescription.String, meta);
            });
    }
}
