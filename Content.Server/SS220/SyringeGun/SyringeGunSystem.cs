// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chemistry.Components;
using Content.Server.Damage.Components;
using Content.Server.Damage.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.SS220.SyringeGun;

namespace Content.Server.SS220.SyringeGun;

public sealed partial class SyringeGunSystem : SharedSyringeGunSystem
{
    [Dependency] private readonly DamageOtherOnHitSystem _damageOtherOnHitSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyringeGunComponent, AddSyringeTemporaryComponentsEvent>(OnAddTemporaryComponents);
    }

    private void OnAddTemporaryComponents(Entity<SyringeGunComponent> ent, ref AddSyringeTemporaryComponentsEvent args)
    {
        var item = args.Uid;
        var (_, gunComp) = ent;

        if (!TryComp<InjectorComponent>(item, out var injector) ||
            !TryComp<TemporarySyringeComponentsComponent>(item, out var temporarySyringeComponents))
        {
            args.Cancelled = true;
            return;
        }

        if (!EnsureComp<SolutionInjectOnEmbedComponent>(item, out var embedComponent))
        {
            embedComponent.Solution = gunComp.SolutionType;
            embedComponent.PierceArmor = gunComp.PierceArmor;
            embedComponent.TransferAmount = injector.MaximumTransferAmount;
            temporarySyringeComponents.Components.Add(embedComponent.GetType());
        }

        if (!EnsureComp<DamageOtherOnHitComponent>(item, out var damageOnHit))
        {
            _damageOtherOnHitSystem.SetComponentStates(item, gunComp.DamageOnHit, gunComp.IgnoreResistances, damageOnHit);
            temporarySyringeComponents.Components.Add(damageOnHit.GetType());
        }
    }
}
