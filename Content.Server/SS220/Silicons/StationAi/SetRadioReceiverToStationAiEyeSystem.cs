using Content.Server.Radio.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.StationAi;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Silicons.StationAi;

public sealed class SetRadioReceiverToStationAiEyeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetRadioReceiverToStationAiEyeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SetRadioReceiverToStationAiEyeComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<SetRadioReceiverToStationAiEyeComponent, StationAiEyeAttachedEvent>(OnEyeAttached);
        SubscribeLocalEvent<SetRadioReceiverToStationAiEyeComponent, StationAiEyeDetachedEvent>(OnEyeDetached);
    }

    private void OnStartup(Entity<SetRadioReceiverToStationAiEyeComponent> entity, ref ComponentStartup args)
    {
        if (TryGetCore(entity, out var core)
            && core.Comp is { }
            && TryComp<IntrinsicRadioReceiverComponent>(entity, out var receiverComponent))
        {
            receiverComponent.ReceiverEntityOverride = core.Comp.RemoteEntity;
        }
    }

    private void OnRemove(Entity<SetRadioReceiverToStationAiEyeComponent> entity, ref ComponentRemove args)
    {
        if (TryComp<IntrinsicRadioReceiverComponent>(entity, out var receiverComponent))
        {
            receiverComponent.ReceiverEntityOverride = null;
        }
    }

    private void OnEyeAttached(Entity<SetRadioReceiverToStationAiEyeComponent> entity, ref StationAiEyeAttachedEvent args)
    {
        if (!TryComp<IntrinsicRadioReceiverComponent>(entity, out var receiverComponent))
            return;
        if (!TryComp<StationAiCoreComponent>(args.AiCore, out var core))
            return;

        receiverComponent.ReceiverEntityOverride = core.RemoteEntity;
    }

    private void OnEyeDetached(Entity<SetRadioReceiverToStationAiEyeComponent> entity, ref StationAiEyeDetachedEvent args)
    {
        if (!TryComp<IntrinsicRadioReceiverComponent>(entity, out var receiverComponent))
            return;

        receiverComponent.ReceiverEntityOverride = null;
    }

    private bool TryGetCore(EntityUid ent, out Entity<StationAiCoreComponent?> core)
    {
        if (!_containerSystem.TryGetContainingContainer((ent, null, null), out var container) ||
            container.ID != StationAiCoreComponent.Container ||
            !TryComp(container.Owner, out StationAiCoreComponent? coreComp) ||
            coreComp.RemoteEntity == null)
        {
            core = (EntityUid.Invalid, null);
            return false;
        }

        core = (container.Owner, coreComp);
        return true;
    }
}
