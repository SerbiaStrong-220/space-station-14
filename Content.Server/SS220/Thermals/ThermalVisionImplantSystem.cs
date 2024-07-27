//EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Thermals;

namespace Content.Server.SS220.Thermals;
/// <summary>
/// Handles enabling of thermal vision when clothing is equipped and disabling when unequipped.
/// </summary>
public sealed class SharedThermalVisionImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionImplantComponent, UseThermalVisionEvent>(OnThermalVisionAction);
    }
    private void OnThermalVisionAction(Entity<ThermalVisionImplantComponent> ent, ref UseThermalVisionEvent args)
    {
        if (HasComp<ThermalVisionComponent>(args.Performer) && ent.Comp.IsAcive)
            RemComp<ThermalVisionComponent>(args.Performer);

        else if (!TryComp<ThermalVisionComponent>(args.Performer, out var thermalVision))
            AddComp(args.Performer, new ThermalVisionComponent(ent.Comp.ThermalVisionRadius));
        else
        {
            thermalVision.ThermalVisionRadius = ent.Comp.ThermalVisionRadius;
            Dirty(args.Performer, thermalVision);
        }

        ent.Comp.IsAcive = !ent.Comp.IsAcive;
    }
}
