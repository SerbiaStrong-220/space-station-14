// Original code github.com/CM-14 Licence MIT, all edits under EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Thermals;

public abstract class SharedThermalVisonSystem : EntitySystem
{
    public override void Initialize()
    {
        //SubscribeLocalEvent<ThermalVisionComponent, ComponentStartup>(OnThermalStartup);
        SubscribeLocalEvent<ThermalVisionComponent, MapInitEvent>(OnThermalMapInit);
        SubscribeLocalEvent<ThermalVisionComponent, AfterAutoHandleStateEvent>(OnThermalHandle);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentRemove>(OnThermalRemove);
    }

    // private void OnThermalStartup(Entity<ThermalVisionComponent> ent, ref ComponentStartup args)
    // {
    //     ThermalVisionChanged(ent);
    // }

    private void OnThermalHandle(Entity<ThermalVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ThermalVisionChanged(ent);
    }

    private void OnThermalMapInit(Entity<ThermalVisionComponent> ent, ref MapInitEvent args)
    {
        ThermalVisionChanged(ent); // leave it here, maybe smn make it roundstart spawn
    }

    private void OnThermalRemove(Entity<ThermalVisionComponent> ent, ref ComponentRemove args)
    {
        ThermalVisionRemoved(ent);
    }

    public void Toggle(Entity<ThermalVisionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            ThermalVisionState.Off => ThermalVisionState.Half,
            ThermalVisionState.Half => ThermalVisionState.Full,
            ThermalVisionState.Full => ThermalVisionState.Off,
            _ => throw new ArgumentOutOfRangeException()
        };

        Dirty(ent);
    }

    protected virtual void ThermalVisionChanged(Entity<ThermalVisionComponent> ent)
    {
    }

    protected virtual void ThermalVisionRemoved(Entity<ThermalVisionComponent> ent)
    {
    }
}
