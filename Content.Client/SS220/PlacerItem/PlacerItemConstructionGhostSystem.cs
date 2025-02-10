// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.SS220.PlacerItem;
using Content.Shared.SS220.PlacerItem.Components;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.SS220.PlacerItem;

public sealed class PlacerItemConstructionGhostSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;

    private string _placementMode = typeof(AlignPlacerItemConstruction).Name;
    private Direction _placementDirection = default;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsPlacerItem = HasComp<PlacerItemComponent>(placerEntity);

        if (_placementManager.Eraser ||
            (placerEntity != null && !placerIsPlacerItem))
            return;

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (!TryComp<HandsComponent>(player, out var hands))
            return;

        var heldEntity = hands.ActiveHand?.HeldEntity;
        if (!TryComp<PlacerItemComponent>(heldEntity, out var comp) || !comp.Active)
        {
            if (placerIsPlacerItem)
            {
                _placementManager.Clear();
                _placementDirection = default;
            }

            return;
        }

        // Update the direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new PlacerItemUpdateDirectionEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        if (heldEntity == placerEntity && placerProto == comp.SpawnProto.Id)
            return;

        var newObjInfo = new PlacementInformation
        {
            MobUid = heldEntity.Value,
            EntityType = comp.ConstructionGhostProto ?? comp.SpawnProto,
            PlacementOption = _placementMode,
            Range = (int)Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = false,
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
}
