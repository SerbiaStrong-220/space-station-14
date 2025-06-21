// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stealth.Components;

namespace Content.Shared.SS220.StealthProvider;
public sealed class SharedProvidedStealthSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProvidedStealthComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ProvidedStealthComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<ProvidedStealthComponent> ent, ref ComponentInit args)
    {
        EnsureComp<StealthComponent>(ent);
        EnsureComp<StealthOnMoveComponent>(ent);
    }

    private void OnRemove(Entity<ProvidedStealthComponent> ent, ref ComponentRemove args)
    {
        //required cause spaming logs
        if (HasComp<StealthOnMoveComponent>(ent))
            RemCompDeferred<StealthOnMoveComponent>(ent);

        if (HasComp<StealthComponent>(ent))
            RemCompDeferred<StealthComponent>(ent);
    }

    public void ProviderRemove(Entity<ProvidedStealthComponent> ent, Entity<StealthProviderComponent> provider)
    {
        ent.Comp.StealthProviders.Remove(provider);
        CheckAmountOfProviders(ent);
    }

    private void CheckAmountOfProviders(Entity<ProvidedStealthComponent> ent)
    {
        if (ent.Comp.StealthProviders.Count > 0)
            return;

        RemComp<ProvidedStealthComponent>(ent);
    }
}
