// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.SS220.SuperMatter.Emitter;
using Content.Shared.Projectiles;
using Content.Server.SS220.SuperMatterCrystal.Components;

namespace Content.Server.SS220.SuperMatter.Emitter;

public sealed class SuperMatterEmitterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperMatterEmitterBoltComponent, SyncSuperMatterBoltStats>(OnSync);
    }
    private void OnSync(Entity<SuperMatterEmitterBoltComponent> entity, ref SyncSuperMatterBoltStats _)
    {
        if (!TryComp<ProjectileComponent>(entity.Owner, out var projectileComponent))
            return;
        var shootAuthorUid = projectileComponent.Shooter;
        if (!TryComp<SuperMatterEmitterExtensionComponent>(shootAuthorUid, out var superMatterEmitter))
            return;
        var consumableComponent = EnsureComp<SuperMatterExtraConsumableComponent>(entity.Owner);

        consumableComponent.AdditionalEnergyOnConsumption =
            SuperMatterEmitterExtensionConsts.GetEnergyFromPower(superMatterEmitter.EnergyToMatterRatio / 100f * superMatterEmitter.PowerConsumption);
        consumableComponent.AdditionalMatterOnConsumption =
            SuperMatterEmitterExtensionConsts.GetMatterFromPower((100 - superMatterEmitter.EnergyToMatterRatio) / 100f * superMatterEmitter.PowerConsumption);
    }
}

public sealed class SyncSuperMatterBoltStats : EntityEventArgs { }
