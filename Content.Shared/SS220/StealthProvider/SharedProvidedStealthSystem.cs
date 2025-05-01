// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Physics.Events;
using Content.Shared.Whitelist;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Shared.SS220.StealthProvider;
public sealed class SharedProvidedStealthSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProvidedStealthComponent, ComponentStartup>(OnStartup);
        //SubscribeLocalEvent<ProvidedStealthComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ProvidedStealthComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<StealthComponent>(ent);
        EnsureComp<StealthOnMoveComponent>(ent);
    }

    private void OnShutdown(Entity<ProvidedStealthComponent> ent, ref ComponentShutdown args)
    {
        //RemComp<StealthComponent>(ent);
        //RemComp<StealthOnMoveComponent>(ent);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

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
