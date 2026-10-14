// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.Silicons.StationAi.CameraMap;

public abstract class SharedStationAiCameraMapSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiCameraMapComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationAiCameraMapComponent, StationAiCameraMapJumpMessage>(OnJump);
        SubscribeLocalEvent<StationAiCameraMapComponent, StationAiCameraMapCrewJumpMessage>(OnCrewJump);
    }

    private void OnMapInit(Entity<StationAiCameraMapComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = Timing.CurTime;
    }

    /// <summary>
    ///     Predicted: the client starts the cooldown at once. Only the server knows camera power and wiring,
    ///     so it alone moves the eye; a refused jump rolls the client cooldown back with the next state.
    /// </summary>
    private void OnJump(Entity<StationAiCameraMapComponent> ent, ref StationAiCameraMapJumpMessage args)
    {
        if (args.Actor != ent.Owner || !IsReady(ent.AsNullable()) || !TryMoveEye(ent, args.Target))
            return;

        StartCooldown(ent);
    }

    private void OnCrewJump(Entity<StationAiCameraMapComponent> ent, ref StationAiCameraMapCrewJumpMessage args)
    {
        if (args.Actor != ent.Owner || !IsReady(ent.AsNullable()) || !TryMoveEyeToCrew(ent, args.Sensor))
            return;

        StartCooldown(ent);
    }

    /// <summary>
    ///     Moves the AI eye to a camera or beacon. The client cannot check the target and assumes success.
    /// </summary>
    protected abstract bool TryMoveEye(Entity<StationAiCameraMapComponent> ai, NetEntity target);

    /// <summary>
    ///     Moves the AI eye to a crew member by their suit sensor. The client assumes success.
    /// </summary>
    protected abstract bool TryMoveEyeToCrew(Entity<StationAiCameraMapComponent> ai, NetEntity sensor);

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
        CooldownChanged(ent);
    }

    /// <summary>
    ///     Lets the client refresh an open window after a predicted cooldown change.
    /// </summary>
    protected virtual void CooldownChanged(Entity<StationAiCameraMapComponent> ent)
    {
    }
}
