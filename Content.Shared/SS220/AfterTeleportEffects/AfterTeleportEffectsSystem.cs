// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.InteractionTeleport;

namespace Content.Shared.SS220.AfterTeleportEffects;

public sealed class AfterTeleportEffectsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterTeleportEffectsComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnTargetTeleported(Entity<AfterTeleportEffectsComponent> ent, ref TargetTeleportedEvent args)
    {
        var target = args.Target;
        //ToDo_SS220 add status effects
    }
}
