// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Text.RegularExpressions;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Robust.Shared.Player;

namespace Content.Server.SS220.RenameStart;

/// <summary>
/// This handles opens the ui to change your name at the beginning of the game. Renaming is necessary for such roles as a clown with a “custom” name
/// </summary>
public sealed class RenameStartSystem : EntitySystem
{
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly AdminFrozenSystem _frozen = default!;
    private static readonly Regex Expressions = new("[^А-Яа-яёЁ0-9' \\-?!,.]");
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RenameStartComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(Entity<RenameStartComponent> ent, ref PlayerAttachedEvent args)
    {
        _frozen.FreezeAndMute(ent.Owner); //prevent players from changing their name after showing up with their initial name

        ChangeName(ent.Owner);
    }

    private void ChangeName(EntityUid entOwner)
    {
        if(!TryComp<ActorComponent>(entOwner, out var actorComp))
            return;

        if(!TryComp<RenameStartComponent>(entOwner, out var renameComp))
            return;

        _quickDialog.OpenDialog(actorComp.PlayerSession,
            Loc.GetString("rename-window-title"),
            description: Loc.GetString("rename-window-desc"),
            Loc.GetString("rename-window-promt"),
            (LongString newName) =>
            {
                if (newName.String.Length <= renameComp.MinChar ||
                    newName.String.Length >= renameComp.MaxChar ||
                    Expressions.IsMatch(newName.String))
                {
                    ChangeName(entOwner);
                    return;
                }

                _meta.SetEntityName(entOwner, newName);

                RemComp<AdminFrozenComponent>(entOwner);

                RemComp<RenameStartComponent>(entOwner);

            }, () =>
            {
                RemComp<AdminFrozenComponent>(entOwner);

                RemComp<RenameStartComponent>(entOwner);
            });
    }
}
