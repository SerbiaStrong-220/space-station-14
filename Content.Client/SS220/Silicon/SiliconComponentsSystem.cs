// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Alert;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.SS220.SiliconComponents;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.SS220.SiliconComponents;

public sealed partial class SiliconComponentsSystem : SharedSiliconComponentsSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly TimeSpan AlertUpdateDelay = TimeSpan.FromSeconds(0.5f);

    private TimeSpan _nextAlertUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponentsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SiliconComponentsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<SiliconComponentsComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateBatteryAlert((ent.Owner, ent.Comp, null));
    }

    private void OnPlayerDetached(Entity<SiliconComponentsComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
    }

    private void UpdateBatteryAlert(Entity<SiliconComponentsComponent, PowerCellSlotComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        if (!_powerCell.TryGetBatteryFromSlot((ent.Owner, ent.Comp2), out var battery))
        {
            _alerts.ShowAlert(ent.Owner, ent.Comp1.NoBatteryAlert);
            return;
        }

        var chargeLevel = (short)MathF.Round(_battery.GetChargeLevel(battery.Value.AsNullable()) * 10f);

        if (chargeLevel == 0 && _powerCell.HasDrawCharge((ent.Owner, null, ent.Comp2)))
            chargeLevel = 1;

        _alerts.ShowAlert(ent.Owner, ent.Comp1.BatteryAlert, chargeLevel);
    }

    public override void UpdateUI(Entity<SiliconComponentsComponent?> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, SiliconUiKey.Key, out var bui))
            bui.Update();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } localPlayer)
            return;

        var curTime = _timing.CurTime;

        if (curTime < _nextAlertUpdate)
            return;

        _nextAlertUpdate = curTime + AlertUpdateDelay;

        if (!TryComp<SiliconComponentsComponent>(localPlayer, out var owner) || !TryComp<PowerCellSlotComponent>(localPlayer, out var slot))
            return;

        UpdateBatteryAlert((localPlayer, owner, slot));
    }
}
