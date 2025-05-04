// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Photocopier.Forms;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.StealthProvider;
public sealed class SharedStealthProviderSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
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

        foreach (var reciever in _entityLookup.GetEntitiesInRange(transform.Coordinates, ent.Comp.Range))
        {
            if (ent.Comp.Whitelist is not null && !_whitelist.IsValid(ent.Comp.Whitelist, reciever))
                continue;

            if (_container.IsEntityOrParentInContainer(reciever))
                continue;

            var prov = EnsureComp<ProvidedStealthComponent>(reciever);
            if (!prov.StealthProviders.Contains(ent))
                prov.StealthProviders.Add(ent);
        }
    }
}
