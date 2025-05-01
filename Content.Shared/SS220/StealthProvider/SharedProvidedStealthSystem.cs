// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Content.Shared.Stealth.Components;

namespace Content.Shared.SS220.StealthProvider;
public sealed class SharedProvidedStealthSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProvidedStealthComponent, ComponentInit>(OnInit);
        //SubscribeLocalEvent<ProvidedStealthComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<ProvidedStealthComponent> ent, ref ComponentInit args)
    {
        EnsureComp<StealthComponent>(ent);
        EnsureComp<StealthOnMoveComponent>(ent);
    }

    private void OnShutdown(Entity<ProvidedStealthComponent> ent, ref ComponentShutdown args)
    {
        //RemComp<StealthComponent>(ent);
        //RemComp<StealthOnMoveComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProvidedStealthComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            CheckProvidersRange(comp);
        }
    }

    private void CheckProvidersRange(ProvidedStealthComponent comp)
    {
        foreach (var povider in comp.StealthProviders)
        {
            //if()
        }
    }
}
