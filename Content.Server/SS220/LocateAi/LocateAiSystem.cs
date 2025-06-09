using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.LocateAi;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.LocateAi;

public sealed class LocateAiSystem : SharedLocateAiSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocateAiComponent, UseInHandEvent>(OnUseInHand);
    }

    public override void Update(float frameTime)
    {
        var queryLocate = EntityQueryEnumerator<LocateAiComponent>();

        while (queryLocate.MoveNext(out var uid, out var locateAiComponent))
        {
            if (!locateAiComponent.IsActive)
            {
                if (locateAiComponent.LastDetected)
                {
                    locateAiComponent.LastDetected = false;
                    RaiseNetworkEvent(new LocateAiEvent(GetNetEntity(uid), false));
                }

                continue;
            }

            var detected =
                _lookup.GetEntitiesInRange<StationAiCoreComponent>(Transform(uid).Coordinates,
                        locateAiComponent.RangeDetection)
                    .Count > 0;

            if (locateAiComponent.LastDetected == detected)
                continue;

            locateAiComponent.LastDetected = detected;
            RaiseNetworkEvent(new LocateAiEvent(GetNetEntity(uid), detected));
        }
    }

    private void OnUseInHand(Entity<LocateAiComponent> ent, ref UseInHandEvent args)
    {
        ent.Comp.IsActive = !ent.Comp.IsActive;

        var message = Loc.GetString(
            ent.Comp.IsActive ? "multitool-syndie-toggle-on" : "multitool-syndie-toggle-off");

        _popup.PopupEntity(message, args.User, args.User);
    }
}
