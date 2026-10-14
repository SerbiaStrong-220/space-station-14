// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.Silicons.StationAi.CameraMap;

/// <summary>
///     Cooldown shared by client and server. Whether a jump succeeds depends on camera power and
///     wiring that only the server knows, so the jump itself happens in the server system.
/// </summary>
public abstract class SharedStationAiCameraMapSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiCameraMapComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<StationAiCameraMapComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = Timing.CurTime;
    }

    public bool IsReady(Entity<StationAiCameraMapComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return Timing.CurTime >= ent.Comp.NextJump;
    }

    /// <summary>
    ///     Returns true once per <see cref="StationAiCameraMapComponent.UpdateInterval"/>.
    /// </summary>
    protected bool UpdateDue(Entity<StationAiCameraMapComponent> ent)
    {
        if (Timing.CurTime < ent.Comp.NextUpdate)
            return false;

        ent.Comp.NextUpdate = Timing.CurTime + ent.Comp.UpdateInterval;
        return true;
    }

    protected void StartCooldown(Entity<StationAiCameraMapComponent> ent)
    {
        ent.Comp.NextJump = Timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);
    }
}
