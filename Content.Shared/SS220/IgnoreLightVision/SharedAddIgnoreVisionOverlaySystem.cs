// Original code github.com/CM-14 Licence MIT, all edits under EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.IgnoreLightVision;

/// <summary>
/// This class contains base logic for adding overlays which inherits <see cref="IgnoreLightVisionOverlayState"/>
/// </summary>
public abstract class SharedAddIgnoreLightVisionOverlaySystem<T> : EntitySystem where T : AddIgnoreLightVisionOverlayComponent
{

    public override void Initialize()
    {
        SubscribeLocalEvent<T, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<T, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<T, ComponentHandleState>(OnHandle);
        SubscribeLocalEvent<T, ComponentGetState>(GetCompState);
    }

    public void Toggle(Entity<T?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            IgnoreLightVisionOverlayState.Off => IgnoreLightVisionOverlayState.Half,
            IgnoreLightVisionOverlayState.Half => IgnoreLightVisionOverlayState.Off,
            _ => throw new ArgumentOutOfRangeException()
        };

        Dirty(ent);
    }

    protected virtual void VisionChanged(Entity<T> ent) { }
    protected virtual void VisionRemoved(Entity<T> ent) { }

    protected virtual void OnMapInit(Entity<T> ent, ref MapInitEvent args)
    {
        VisionChanged((ent.Owner, ent.Comp));
    }
    protected virtual void OnComponentRemove(Entity<T> ent, ref ComponentRemove args)
    {
        VisionRemoved((ent.Owner, ent.Comp));
    }

    private void OnHandle(Entity<T> ent, ref ComponentHandleState args)
    {
        if (args.Current is not AddIgnoreLightVisionOverlayState state)
            return;

        ent.Comp.State = state.State;
        ent.Comp.VisionRadius = state.VisionRadius;
        ent.Comp.HighSensitiveVisionRadius = state.HighSensitiveVisionRadius;

        VisionChanged((ent.Owner, ent.Comp));
    }

    private void GetCompState(Entity<T> entity, ref ComponentGetState args)
    {
        args.State = new AddIgnoreLightVisionOverlayState
        {
            State = entity.Comp.State,
            VisionRadius = entity.Comp.VisionRadius,
            HighSensitiveVisionRadius = entity.Comp.HighSensitiveVisionRadius,
        };
    }
}
