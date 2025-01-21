// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics;
using System.Linq;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Singularity.Components;
using Content.Shared.SS220.SuperMatter.Emitter;
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SuperMatter.Emitter;


public sealed class SuperMatterEmitterExtensionSystem : EntitySystem
{
    [Dependency] EmitterSystem _emitter = default!;
    [Dependency] IPrototypeManager _prototypeManager = default!;
    [Dependency] UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperMatterEmitterExtensionComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SuperMatterEmitterExtensionComponent, SuperMatterEmitterExtensionValueMessage>(OnApplyMessage);
        SubscribeLocalEvent<SuperMatterEmitterExtensionComponent, SuperMatterEmitterExtensionEmitterActivateMessage>(OnEmitterActivateMessage);
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
        entity.Comp.PowerConsumption = Math.Min(16384, args.PowerConsumption);
        entity.Comp.EnergyToMatterRatio = Math.Clamp(args.EnergyToMatterRatio, 0, 100);

        UpdateCorrespondingComponents(entity.Owner, entity.Comp, out var emitterComponent);

        Dirty(entity);
        if (emitterComponent != null)
            Dirty(entity.Owner, emitterComponent);

        UpdateBUI(entity);
    }

    /// <summary>
    /// Updates entities BUI with current parameters
    /// </summary>
    /// <param name="entity"></param>
    public void UpdateBUI(Entity<SuperMatterEmitterExtensionComponent> entity)
    {
        var state = new SuperMatterEmitterExtensionUpdate(entity.Comp.PowerConsumption, entity.Comp.EnergyToMatterRatio);

        _userInterface.SetUiState(entity.Owner, SuperMatterEmitterExtensionUiKey.Key, state);
    }

    private void UpdateCorrespondingComponents(EntityUid uid, SuperMatterEmitterExtensionComponent comp, out EmitterComponent? emitterComponent)
    {
        if (!TryComp<EmitterComponent>(uid, out emitterComponent))
        {
            Log.Debug($"SM Emitter Extension exist in entity, but it doesnt have {nameof(EmitterComponent)}");
            return;
        }
        emitterComponent.PowerUseActive = comp.PowerConsumption;
    }

    private void OnEmitterActivateMessage(Entity<SuperMatterEmitterExtensionComponent> entity, ref SuperMatterEmitterExtensionEmitterActivateMessage args)
    {
        if (!TryComp<EmitterComponent>(entity, out var emitterComponent))
        {
            Log.Debug($"SM Emitter Extension exist in entity, but it doesnt have {nameof(EmitterComponent)}");
            return;
        }

        var users = _userInterface.GetActors(entity.Owner, SuperMatterEmitterExtensionUiKey.Key);
        Debug.Assert(users.Count() == 1);

        _emitter.TryActivate((entity.Owner, emitterComponent), users.First());
        Dirty<EmitterComponent>((entity.Owner, emitterComponent));
    }
}
