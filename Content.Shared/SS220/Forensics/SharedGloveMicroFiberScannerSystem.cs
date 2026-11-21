// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Predicts the scanner's mode switch and displays its current collection mode.
/// </summary>
public sealed class SharedGloveMicroFiberScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GloveMicroFiberScannerComponent, ExaminedEvent>(OnExamined);
        SubscribeAllEvent<GloveMicroFiberScannerSetModeEvent>(OnSetMode);
    }

    private void OnSetMode(GloveMicroFiberScannerSetModeEvent msg, EntitySessionEventArgs args)
    {
        if (msg.Mode != GloveMicroFiberScannerMode.SurfaceTraces && msg.Mode != GloveMicroFiberScannerMode.OwnSample)
            return;

        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryGetEntity(msg.Scanner, out var scanner))
            return;

        if (!TryComp<GloveMicroFiberScannerComponent>(scanner.Value, out var component))
            return;

        if (!_hands.IsHolding(user.Value, scanner))
            return;

        if (!_blocker.CanInteract(user.Value, scanner))
            return;

        if (!_blocker.CanUseHeldEntity(user.Value, scanner.Value))
            return;

        SetMode((scanner.Value, component), msg.Mode, user.Value);
    }

    private void SetMode(Entity<GloveMicroFiberScannerComponent> ent, GloveMicroFiberScannerMode mode, EntityUid user)
    {
        if (ent.Comp.Mode == mode)
            return;

        ent.Comp.Mode = mode;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("glove-micro-fiber-scanner-mode-selected", ("mode", GetModeName(mode))),
            ent, user);
    }

    private void OnExamined(Entity<GloveMicroFiberScannerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString("glove-micro-fiber-scanner-mode-selected", ("mode", GetModeName(ent.Comp.Mode))));
        args.PushText(Loc.GetString("glove-micro-fiber-scanner-switch-hint"));
    }

    private string GetModeName(GloveMicroFiberScannerMode mode)
    {
        return Loc.GetString(mode == GloveMicroFiberScannerMode.SurfaceTraces
            ? "glove-micro-fiber-scanner-mode-surface"
            : "glove-micro-fiber-scanner-mode-own");
    }
}
