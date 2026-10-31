// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.TapeRecorder;

namespace Content.Shared.SS220.BodyCamera;

public abstract partial class SharedBodyCameraPowerSystem : EntitySystem
{
    [Dependency] protected SharedBatterySystem Battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraPowerComponent, TapeRecorderModeChangeAttemptEvent>(OnModeChangeAttempt);
    }

    private void OnModeChangeAttempt(Entity<BodyCameraPowerComponent> ent, ref TapeRecorderModeChangeAttemptEvent args)
    {
        if (!HasCharge(ent))
            args.Cancelled = true;
    }

    protected bool HasCharge(EntityUid uid)
    {
        return Battery.GetCharge(uid) > 0f;
    }
}
