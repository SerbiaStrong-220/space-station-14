// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chemistry.Components;
using Content.Server.Damage.Components;
using Content.Server.Damage.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.SS220.SyringeGun;

namespace Content.Server.SS220.SyringeGun;

public sealed partial class SyringeGunSystem : SharedSyringeGunSystem
{
    [Dependency] private readonly DamageOtherOnHitSystem _damageOtherOnHitSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyringeGunComponent, AddSyringeTemporaryComponentsEvent>(OnAddTemporaryComponents);
    }

    private void OnAddTemporaryComponents(Entity<SyringeGunComponent> ent, ref AddSyringeTemporaryComponentsEvent args)
    {
        var item = args.Uid;
        var (_, gunComp) = ent;

        if (!TryComp<TemporarySyringeComponentsComponent>(item, out var temporarySyringeComponents) ||
            !_solutionContainerSystem.TryGetSolution(item, gunComp.SolutionType, out _, out var solution))
        {
            args.Cancelled = true;
            return;
        }

        if (!EnsureComp<SolutionInjectOnEmbedComponent>(item, out var embedComponent))
        {
            embedComponent.Solution = gunComp.SolutionType;
            embedComponent.PierceArmor = gunComp.PierceArmor;
            embedComponent.TransferAmount = solution.MaxVolume;
            temporarySyringeComponents.Components.Add(embedComponent.GetType());
        }

        if (!EnsureComp<DamageOtherOnHitComponent>(item, out var damageOnHit))
        {
            _damageOtherOnHitSystem.SetComponentStates(item, gunComp.DamageOnHit, gunComp.IgnoreResistances, damageOnHit);
            temporarySyringeComponents.Components.Add(damageOnHit.GetType());
        }
    }
}
