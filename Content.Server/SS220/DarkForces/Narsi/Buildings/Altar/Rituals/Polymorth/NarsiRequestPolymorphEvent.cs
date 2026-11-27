using Content.Shared.Polymorph;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Polymorth;

public sealed class NarsiRequestPolymorphEvent : CancellableEntityEventArgs
{
    public EntityUid Target;
    public PolymorphConfiguration Configuration;
    public bool ReturnToAltar;


    public NarsiRequestPolymorphEvent(EntityUid target, PolymorphConfiguration configuration, bool returnToAltar)
    {
        Target = target;
        Configuration = configuration;
        ReturnToAltar = returnToAltar;
    }
}
