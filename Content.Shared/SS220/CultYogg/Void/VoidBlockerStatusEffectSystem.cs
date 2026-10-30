// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusEffectNew;

namespace Content.Shared.SS220.CultYogg.Void;

public sealed class VoidBlockerStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoidBlockerStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<VoidBlockerStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
    }

    private void OnStatusEffectApplied(Entity<VoidBlockerStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<VoidBlockerComponent>(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<VoidBlockerStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RemCompDeferred<VoidBlockerComponent>(args.Target);
    }
}
