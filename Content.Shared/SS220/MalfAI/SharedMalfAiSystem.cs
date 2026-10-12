// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction;

namespace Content.Shared.SS220.MalfAI;

public sealed class SharedMalfAiSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MalfAiActorComponent, AccessibleOverrideEvent>(OnAccessible);
    }

    private void OnAccessible(Entity<MalfAiActorComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (args.User != ent.Owner)
            return;

        if (!HasComp<MalfAiHackableComponent>(args.Target)
            && !HasComp<MalfAiTargetableComponent>(args.Target))
            return;

        args.Handled = true;
        args.Accessible = true;
    }
}
