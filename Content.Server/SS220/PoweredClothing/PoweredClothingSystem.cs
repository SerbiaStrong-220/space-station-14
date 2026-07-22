// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Item.ItemToggle;
using Content.Shared.PowerCell;
using Content.Shared.SS220.PhysicalParameters;
using Content.Shared.SS220.PoweredClothing;
using Robust.Shared.Timing;

namespace Content.Server.SS220.PhysicalParameters;

public sealed class PoweredClothingSystem : SharedPoweredClothingSystem
{
    [Dependency] private readonly PowerCellSystem _cellSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActivePoweredClothingComponent, PoweredClothingComponent>();

        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (_timing.CurTime < active.TargetTime)
                continue;

            if (!_cellSystem.TryUseCharge(comp.PowerSource, comp.DrawRate))
            {
                RemComp<ActivePoweredClothingComponent>(uid);

                var ev = new PoweredClothingTurnedOffEvent();
                RaiseLocalEvent(uid, ref ev);

                _itemToggle.TryDeactivate(uid, predicted: false);

                return;
            }
        }
    }
}
