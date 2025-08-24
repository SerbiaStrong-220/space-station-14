// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Shared.SS220.StealthProvider;
public sealed class SharedStealthProviderSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedProvidedStealthSystem _provided = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthProviderComponent, StealthProviderStatusChanged>(OnEnabilityChange);
        SubscribeLocalEvent<StealthProviderComponent, ComponentRemove>(OnCompRemove);
    }

    private void OnEnabilityChange(Entity<StealthProviderComponent> ent, ref StealthProviderStatusChanged args)
    {
        ent.Comp.Enabled = args.Enabled;

        if (!ent.Comp.Enabled)
            DisableAllProvidedStealth(ent);
    }

    private void OnCompRemove(Entity<StealthProviderComponent> ent, ref ComponentRemove args)
    {
        DisableAllProvidedStealth(ent);
    }

    private void DisableAllProvidedStealth(Entity<StealthProviderComponent> ent)
    {
        foreach (var disEnts in ent.Comp.ProvidedEntities)
        {
            _provided.ProviderRemove(disEnts, ent);
        }

        ent.Comp.ProvidedEntities.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StealthProviderComponent>();

        while (query.MoveNext(out var ent, out var comp))
        {
            if (!comp.Enabled)
                return;

            ProvideStealthInRange((ent, comp));
        }
    }

    private void ProvideStealthInRange(Entity<StealthProviderComponent> ent)
    {
        var transform = Transform(ent);

        var entsToDisable = ent.Comp.ProvidedEntities.ToList();

        foreach (var reciever in _entityLookup.GetEntitiesInRange(transform.Coordinates, ent.Comp.Range))
        {
            if (ent.Comp.Whitelist is not null && !_whitelist.IsValid(ent.Comp.Whitelist, reciever))
                continue;

            if (_container.IsEntityOrParentInContainer(reciever))
                continue;

            var prov = EnsureComp<ProvidedStealthComponent>(reciever);
            entsToDisable.Remove((reciever, prov));

            if (!prov.StealthProviders.Contains(ent))
            {
                prov.StealthProviders.Add(ent);
                ent.Comp.ProvidedEntities.Add((reciever, prov));
            }
        }

        foreach (var disEnts in entsToDisable)
        {
            _provided.ProviderRemove(disEnts, ent);
            ent.Comp.ProvidedEntities.Remove(disEnts);
        }
    }
}
