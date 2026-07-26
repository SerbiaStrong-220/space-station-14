// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Ipc;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Ipc;

public sealed partial class IpcSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    private static readonly TimeSpan AlertUpdateDelay = TimeSpan.FromSeconds(0.5f);
    private TimeSpan _nextAlertUpdate = TimeSpan.Zero;

    /// <summary>
    /// Multiply the charge fraction by 10 to get the charge percentage
    /// </summary>
    private const float ChargeFracMult = 10f;

    [Dependency] private EntityQuery<IpcComponent> _ipcQuery = default!;
    [Dependency] private EntityQuery<PowerCellSlotComponent> _slotQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IpcComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<IpcComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<IpcComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<IpcComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnPlayerAttached(Entity<IpcComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateBatteryAlert((ent.Owner, ent.Comp, null));
    }

    private void OnPlayerDetached(Entity<IpcComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
    }

    private void OnInserted(Entity<IpcComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateBatteryAlert((ent.Owner, ent.Comp, null));
    }

    private void OnRemoved(Entity<IpcComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateBatteryAlert((ent.Owner, ent.Comp, null));
    }

    private void UpdateBatteryAlert(Entity<IpcComponent, PowerCellSlotComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, false))
            return;

        if (!_powerCell.TryGetBatteryFromSlot((ent.Owner, ent.Comp2), out var battery))
        {
            _alerts.ShowAlert(ent.Owner, ent.Comp1.NoBatteryAlert);
            return;
        }

        // Alert levels from 0 to 10.
        var chargeLevel = (short)MathF.Round(_battery.GetChargeLevel(battery.Value.AsNullable()) * ChargeFracMult);

        // we make sure 0 only shows if they have absolutely no battery.
        // also account for floating point imprecision
        if (chargeLevel == 0 && _powerCell.HasDrawCharge((ent.Owner, null, ent.Comp2)))
        {
            chargeLevel = 1;
        }

        _alerts.ShowAlert(ent.Owner, ent.Comp1.BatteryAlert, chargeLevel);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not { } localPlayer)
            return;

        var curTime = _timing.CurTime;

        if (curTime < _nextAlertUpdate)
            return;

        _nextAlertUpdate += AlertUpdateDelay;

        if (!_ipcQuery.TryComp(localPlayer, out var ipc) || !_slotQuery.TryComp(localPlayer, out var slot))
            return;

        UpdateBatteryAlert((localPlayer, ipc, slot));
    }
}
