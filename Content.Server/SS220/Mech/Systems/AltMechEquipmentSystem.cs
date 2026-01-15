using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Components;

namespace Content.Server.SS220.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
/// </summary>
public sealed class AltMechpartsSystem : EntitySystem
{
    [Dependency] private readonly AltMechSystem _mech = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechPartComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechPartComponent, InsertPartEvent>(OnInsertPart);
    }

    private void OnUsed(EntityUid uid, MechPartComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        if (mechComp.Broken)
            return;

        //SS220-AddMechToClothing-start
        //Why does this check exist if it cannot be called?
        // if (args.User == mechComp.PilotSlot.ContainedEntity)
        //     return;
        //SS220-AddMechToClothing-end

        if (mechComp.EquipmentContainer.ContainedEntities.Count >= mechComp.MaxEquipmentAmount)
            return;

        if (_whitelistSystem.IsWhitelistFail(mechComp.EquipmentWhitelist, args.Used))
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", uid)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.InstallDuration, new InsertPartEvent(), uid, target: mech, used: uid)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnInsertPart(EntityUid uid, MechPartComponent component, InsertPartEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", uid)), args.Args.Target.Value);
        _mech.InsertEquipment(args.Args.Target.Value, uid);

        args.Handled = true;
    }
}
