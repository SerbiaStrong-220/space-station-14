// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.AstralLeap;

public sealed partial class AstralLeapSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AstralLeapComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AstralLeapComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<AstralLeapComponent, AstralLeapActionEvent>(OnAstralLeapAction);
        SubscribeLocalEvent<AstralLeapComponent, AstralLeapDoAfterEvent>(OnAstralLeapDoAfter);

        SubscribeLocalEvent<AstralLeapComponent, PolymorphedEvent>(OnPolymorphed);
    }

    private void OnMapInit(Entity<AstralLeapComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.AstralActionEntity, ent.Comp.AstralAction);
    }

    private void OnShutdown(Entity<AstralLeapComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.AstralActionEntity);
    }

    private void OnAstralLeapAction(Entity<AstralLeapComponent> ent, ref AstralLeapActionEvent args)
    {
        EntityUid? currentGrid = Transform(ent).GridUid;

        if (ent.Comp.GridsBlacklist != null && _whitelist.IsValid(ent.Comp.GridsBlacklist, currentGrid))
        {
            _popup.PopupClient(Loc.GetString("astral-grid-blocked"), ent, ent);
            return;
        }

        if (ent.Comp.AstralLeapDoAfterTime is null)
        {
            SendToAstral(ent);
            return;
        }

        var astralDoAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.AstralLeapDoAfterTime.Value, new AstralLeapDoAfterEvent(), ent)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(astralDoAfter);
    }

    private void OnAstralLeapDoAfter(Entity<AstralLeapComponent> ent, ref AstralLeapDoAfterEvent args)
    {
        SendToAstral(ent);
    }

    private void SendToAstral(Entity<AstralLeapComponent> ent)
    {
        var ev = new PolymorphActionEvent(ent.Comp.AstralEnt);
        RaiseLocalEvent(ent, ev);
    }

    private void OnPolymorphed(Entity<AstralLeapComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert)
            return;

        _actions.StartUseDelay(ent.Comp.AstralActionEntity);
    }
}
