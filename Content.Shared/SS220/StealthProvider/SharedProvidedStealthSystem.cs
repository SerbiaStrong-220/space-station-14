// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Containers;
using Content.Shared.Interaction.Events;

namespace Content.Shared.SS220.StealthProvider;
public sealed class SharedProvidedStealthSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProvidedStealthComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ProvidedStealthComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ProvidedStealthComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ProvidedStealthComponent, BeingUsedAttemptEvent>(OnUseAttempt);
    }

    private void OnInit(Entity<ProvidedStealthComponent> ent, ref ComponentInit args)
    {
        EnsureComp<StealthComponent>(ent);
        EnsureComp<StealthOnMoveComponent>(ent);
    }

    private void OnShutdown(Entity<ProvidedStealthComponent> ent, ref ComponentShutdown args)
    {
        RemComp<StealthComponent>(ent);
        RemComp<StealthOnMoveComponent>(ent);
    }
    private void OnEntInserted(Entity<ProvidedStealthComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RemComp<ProvidedStealthComponent>(ent);
    }

    private void OnUseAttempt(Entity<ProvidedStealthComponent> ent, ref BeingUsedAttemptEvent args)
    {
        RemComp<ProvidedStealthComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProvidedStealthComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            CheckProvidersRange((ent, comp));
        }
    }

    private void CheckProvidersRange(Entity<ProvidedStealthComponent> ent)
    {
        foreach (var povider in ent.Comp.StealthProviders)
        {
            if (!_physics.TryGetDistance(povider, ent, out var distance))
                return;

            if (distance > povider.Comp.Range)
            {
                ent.Comp.StealthProviders.Remove(povider);
                CheckAmountOfProviders(ent);
                return;
            }
        }
    }

    private void CheckAmountOfProviders(Entity<ProvidedStealthComponent> ent)
    {
        if (ent.Comp.StealthProviders.Count > 0)
            return;

        RemComp<ProvidedStealthComponent>(ent);
    }
}
