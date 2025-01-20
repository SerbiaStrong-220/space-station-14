// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Popups;
using Content.Server.SS220.MindSlave.DisfunctionComponents;
using Content.Shared.Wieldable;

namespace Content.Server.SS220.MindSlave.DisfunctionSystem;

public sealed class WieldUnabilitySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WieldUnabilityComponent, WieldAttemptEvent>(OnWieldAttempt);
    }

    private void OnWieldAttempt(Entity<WieldUnabilityComponent> entity, ref WieldAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        _popup.PopupCursor(Loc.GetString("unable-to-wield", ("user", entity.Owner)), entity);
        args.Cancel();
    }
}
