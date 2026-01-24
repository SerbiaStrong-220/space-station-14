using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.Whitelist;

namespace Content.Server.SS220.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
/// </summary>
public sealed class MechPartSystem : EntitySystem
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
        SubscribeLocalEvent<MechPartComponent, MechPartInsertedEvent>(OnPartInserted);
        SubscribeLocalEvent<MechChassisComponent, MechPartInsertedEvent>(OnChassisInserted);
        //SubscribeLocalEvent<MechPartComponent, InsertPartEvent>(OnOpticsInserted);
        //SubscribeLocalEvent<MechPartComponent, InsertPartEvent>(OnArmInserted);
    }

    private void OnUsed(Entity<MechPartComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        if (mechComp.Broken)
            return;

        if (args.User == mechComp.PilotSlot.ContainedEntity)
            return;

        if (ent.Comp.EquipmentContainer.ContainedEntities.Count >= ent.Comp.MaxEquipmentAmount)
            return;

        if (_whitelistSystem.IsWhitelistFail(ent.Comp.EquipmentWhitelist, args.Used))
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", ent.Owner)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallDuration, new InsertPartEvent(), ent.Owner, target: mech, used: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnChassisInserted(Entity<MechChassisComponent> ent, ref MechPartInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        mechComp.OverallBaseMovementSpeed = ent.Comp.BaseMovementSpeed;
    }

    private void OnPartInserted(Entity<MechPartComponent> ent, ref MechPartInsertedEvent args)
    {

    }

    private void OnInsertPart(EntityUid uid, MechPartComponent component, InsertPartEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", uid)), args.Args.Target.Value);
        _mech.InsertPart(args.Args.Target.Value, uid);

        args.Handled = true;
    }
}
