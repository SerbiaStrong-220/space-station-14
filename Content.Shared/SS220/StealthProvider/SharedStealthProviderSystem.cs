// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Physics.Events;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.StealthProvider;
public sealed class SharedStealthProviderSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthProviderComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<StealthProviderComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(Entity<StealthProviderComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.StealthFixtureId)
            return;

        if (ent.Comp.Whitelist == null)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.OtherEntity))
            return;

        EnsureComp<ProvidedStealthComponent>(args.OtherEntity);
    }

    private void OnEndCollide(Entity<StealthProviderComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.StealthFixtureId)
            return;

        if (ent.Comp.Whitelist == null)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.OtherEntity))
            return;

        RemComp<ProvidedStealthComponent>(args.OtherEntity);
    }
}
