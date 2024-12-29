// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Singularity.Components;
using Content.Shared.SS220.SuperMatter.Emitter;
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SuperMatter.Emitter;


public sealed class SuperMatterEmitterExtensionSystem : EntitySystem
{
    [Dependency] IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperMatterEmitterExtensionComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SuperMatterEmitterExtensionComponent, SuperMatterEmitterExtensionValueMessage>(OnApplyMessage);
    }

    private void OnComponentInit(Entity<SuperMatterEmitterExtensionComponent> entity, ref ComponentInit args)
    {
        var emitterComponent = EnsureComp<EmitterComponent>(entity.Owner);

        emitterComponent.PowerUseActive = entity.Comp.PowerConsumption;
        var boltProto = _prototypeManager.Index<EntityPrototype>(emitterComponent.BoltType);
        if (!boltProto.Components.ContainsKey("SuperMatterEmitterBolt"))
            Log.Debug($"Added SM Emitter Extension to entity, but its EmitterComponent.BoltType dont have {nameof(SuperMatterEmitterBoltComponent)}");
    }
    private void OnApplyMessage(Entity<SuperMatterEmitterExtensionComponent> entity, ref SuperMatterEmitterExtensionValueMessage args)
    {
        entity.Comp.PowerConsumption = args.PowerConsumption;
        entity.Comp.EnergyToMatterRatio = args.EnergyToMatterRatio;

        UpdateCorrespondingComponents(entity.Owner, entity.Comp);
        Dirty(entity);
    }
    private void UpdateCorrespondingComponents(EntityUid uid, SuperMatterEmitterExtensionComponent comp)
    {
        if (!TryComp<EmitterComponent>(uid, out var emitterComponent))
        {
            Log.Debug($"SM Emitter Extension exist in entity, but it doesnt have {nameof(EmitterComponent)}");
            return;
        }
        emitterComponent.PowerUseActive = comp.PowerConsumption;
    }
}
