using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.LocateAi;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.LocateAi;

public sealed class LocateAiSystem : SharedLocateAiSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocateAiComponent, UseInHandEvent>(OnUseInHand);
    }

    public override void Update(float frameTime)
    {
        var queryAi = EntityQueryEnumerator<StationAiCoreComponent>();
        var queryLocate = EntityQueryEnumerator<LocateAiComponent>();

        while (queryAi.MoveNext(out _, out var component))
        {
            if (component.RemoteEntity == null)
                continue;

            var remotePosition = _transform.GetWorldPosition(component.RemoteEntity.Value);

            while (queryLocate.MoveNext(out var locate, out var locateAiComponent))
            {
                if (!locateAiComponent.IsActive)
                {
                    if (locateAiComponent.LastDetected)
                    {
                        locateAiComponent.LastDetected = false;
                        RaiseNetworkEvent(new LocateAiEvent(GetNetEntity(locate), false));
                    }

                    continue;
                }

                var distance = (_transform.GetWorldPosition(locate) - remotePosition).Length();

                var detected = distance <= locateAiComponent.RangeDetection;

                if (locateAiComponent.LastDetected == detected)
                    continue;

                locateAiComponent.LastDetected = detected;
                RaiseNetworkEvent(new LocateAiEvent(GetNetEntity(locate), detected));
            }
        }
    }

    private void OnUseInHand(Entity<LocateAiComponent> ent, ref UseInHandEvent args)
    {
        ent.Comp.IsActive = !ent.Comp.IsActive;
        _popup.PopupEntity(Loc.GetString("multitool-syndie-toggle"), args.User, args.User);
    }
}
