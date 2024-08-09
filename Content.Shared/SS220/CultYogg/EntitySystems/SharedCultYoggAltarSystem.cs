// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle.Components;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Network;

namespace Content.Shared.SS220.CultYogg;

public abstract class SharedCultYoggAltarSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggAltarComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<CultYoggSacrificialComponent, BeingPulledAttemptEvent>(OnBeingPulled);
    }

    private void OnBeingPulled(Entity<CultYoggSacrificialComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (TryComp<BuckleComponent>(ent, out var buckleComp) && HasComp<CultYoggAltarComponent>(buckleComp.BuckledTo))
            args.Cancel();
    }

    private void OnBuckleAttempt(Entity<CultYoggAltarComponent> ent, ref BuckleAttemptEvent args)
    {
        if (args.User == null)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Buckle))
        {
            args.Cancelled = true;
            return;
        }

        if (!HasComp<CultYoggSacrificialComponent>(args.Buckle))
        {
            args.Cancelled = true;

            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-buckle-attempt", ("user", args.Buckle)),
                 args.Buckle, (EntityUid) args.User, PopupType.SmallCaution);
        }

        if (TryComp<BuckleComponent>(args.User, out var buckleComp) && buckleComp.Buckled)
            args.Cancelled = true;
    }

    protected void UpdateAppearance(EntityUid uid, CultYoggAltarComponent? altarComp = null,
    AppearanceComponent? appearanceComp = null)
    {
        if (!Resolve(uid, ref altarComp))
            return;

        if (!Resolve(uid, ref appearanceComp))
            return;

        _appearance.SetData(uid, CultYoggAltarComponent.CultYoggAltarVisuals.Sacrificed, altarComp.Used, appearanceComp);
    }
}
