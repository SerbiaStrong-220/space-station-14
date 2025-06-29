// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Sacraficials;

namespace Content.Shared.SS220.CultYogg.Altar;

public abstract partial class SharedCultYoggAltarSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggAltarComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<CultYoggAltarComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<CultYoggAltarComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);

        SubscribeLocalEvent<CultYoggAltarComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUnanchorAttempt(Entity<CultYoggAltarComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (ent.Comp.Used)
            args.Cancel();
    }

    private void OnUnstrapAttempt(Entity<CultYoggAltarComponent> ent, ref UnstrapAttemptEvent args)
    {
        if (args.Buckle == args.User)
            args.Cancelled = true;
    }

    private void OnStrapAttempt(Entity<CultYoggAltarComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.User == null)
            return;

        if (ent.Comp.Used)
            return;

        if (!HasComp<CultYoggSacrificialComponent>(args.Buckle))
        {
            args.Cancelled = true;
            _popup.PopupClient (Loc.GetString("cult-yogg-buckle-attempt", ("user", args.Buckle)),
                 args.Buckle, args.User.Value, PopupType.SmallCaution);
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

    private void OnExamined(Entity<CultYoggAltarComponent> uid, ref ExaminedEvent args)
    {
        if (!uid.Comp.Used)
            return;

        args.PushMarkup($"[color=green]{Loc.GetString("cult-yogg-altar-used", ("ent", uid))}[/color]");
    }
}
