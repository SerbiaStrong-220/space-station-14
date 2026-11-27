using Content.Server.Cuffs;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.Stunnable;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    [Dependency] private readonly CuffableSystem _cuffableSystem = default!;

    private void InitializeCuff()
    {
        SubscribeLocalEvent<NarsiCultistComponent, NarsiCultistCuffEvent>(OnCuffEvent);
    }

    private void OnCuffEvent(EntityUid uid, NarsiCultistComponent component, NarsiCultistCuffEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<StunnedComponent>(args.Target))
            return;

        var coords = Transform(args.Performer).Coordinates;
        var handcuffs = Spawn("HandcuffsCult", coords);

        if (!_handsSystem.TryPickupAnyHand(args.Performer, handcuffs))
        {
            QueueDel(handcuffs);
            _popupSystem.PopupEntity(Loc.GetString("narsi-cuff-free-hand"), uid, uid);
            return;
        }

        if (!_cuffableSystem.TryCuffing(args.Performer, args.Target, handcuffs))
        {
            QueueDel(handcuffs);
            return;
        }
        OnCultistAbility(uid, args);
        args.Handled = true;
    }
}
