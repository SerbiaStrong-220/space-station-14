// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.DisplacementMap;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.SS220.Body;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Body;

/// <summary>
/// Draws an organ outside the body from its body appearance: the organ's layer with the skin color,
/// the organs attached to it and the markings of all of them, in the same order as on the body.
/// </summary>
public sealed partial class SeveredOrganVisualsSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private MarkingManager _marking = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeveredOrganVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SeveredOrganVisualsComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<SeveredOrganVisualsComponent, EntInsertedIntoContainerMessage>(OnAttachedInserted);
        SubscribeLocalEvent<SeveredOrganVisualsComponent, EntRemovedFromContainerMessage>(OnAttachedRemoved);
    }

    private void OnStartup(Entity<SeveredOrganVisualsComponent> ent, ref ComponentStartup args)
    {
        if (!IsInBody(ent))
            Redraw(ent);
    }

    private void OnRemoved(Entity<SeveredOrganVisualsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            Redraw(ent);
    }

    private void OnAttachedInserted(Entity<SeveredOrganVisualsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == LimbComponent.ContainerId && !IsInBody(ent))
            Redraw(ent);
    }

    private void OnAttachedRemoved(Entity<SeveredOrganVisualsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == LimbComponent.ContainerId && !IsInBody(ent) && !TerminatingOrDeleted(ent))
            Redraw(ent);
    }

    /// <summary>
    /// Redraws the severed part the organ is drawn on: the organ itself or the limb it is attached to.
    /// A new attached organ may get into the limb's container before its own appearance state is applied.
    /// </summary>
    public void OrganAppearanceChanged(EntityUid organ)
    {
        if (TryComp<SeveredOrganVisualsComponent>(organ, out var visuals))
        {
            if (!IsInBody(organ))
                Redraw((organ, visuals));
            return;
        }

        if (_container.TryGetContainingContainer(organ, out var container)
            && container.ID == LimbComponent.ContainerId
            && TryComp(container.Owner, out visuals)
            && !IsInBody(container.Owner))
            Redraw((container.Owner, visuals));
    }

    // container membership instead of OrganComponent.Body: within one state the body field and the containers arrive in any order
    private bool IsInBody(EntityUid organ)
    {
        return _container.TryGetContainingContainer(organ, out var container) && container.ID == BodyComponent.ContainerID;
    }

    private void Redraw(Entity<SeveredOrganVisualsComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var spriteComp) || !TryComp<VisualOrganComponent>(ent, out var visual))
            return;

        var sprite = new Entity<SpriteComponent>(ent, spriteComp);
        foreach (var layerId in ent.Comp.AddedLayers)
        {
            _displacement.EnsureDisplacementIsNotOnSprite(sprite, layerId);
            if (_sprite.LayerMapTryGet(sprite.AsNullable(), layerId, out var index, false))
            {
                _sprite.LayerMapRemove(sprite.AsNullable(), layerId);
                _sprite.RemoveLayer(sprite.AsNullable(), index);
            }
        }
        ent.Comp.AddedLayers.Clear();

        _sprite.LayerSetData(sprite.AsNullable(), 0, visual.Data);

        var attached = _container.TryGetContainer(ent, LimbComponent.ContainerId, out var container)
            ? container.ContainedEntities
            : [];

        foreach (var organ in attached)
        {
            if (TryComp<VisualOrganComponent>(organ, out var attachedVisual))
                AddLayer(ent, sprite, attachedVisual.Data);
        }

        AddMarkings(ent, sprite, ent.Owner);
        foreach (var organ in attached)
        {
            AddMarkings(ent, sprite, organ);
        }
    }

    private void AddLayer(Entity<SeveredOrganVisualsComponent> ent, Entity<SpriteComponent> sprite, PrototypeLayerData data)
    {
        TrackLayer(ent, sprite, _sprite.AddLayer(sprite.AsNullable(), data, null));
    }

    private string TrackLayer(Entity<SeveredOrganVisualsComponent> ent, Entity<SpriteComponent> sprite, int index)
    {
        var layerId = $"severed-organ-{ent.Comp.AddedLayers.Count}";
        _sprite.LayerMapSet(sprite.AsNullable(), layerId, index);
        ent.Comp.AddedLayers.Add(layerId);
        return layerId;
    }

    private void AddMarkings(Entity<SeveredOrganVisualsComponent> ent, Entity<SpriteComponent> sprite, EntityUid organ)
    {
        if (!TryComp<VisualOrganMarkingsComponent>(organ, out var markings))
            return;

        var displacements = markings.MarkingsDisplacement;
        foreach (var layerMarkings in markings.Markings.Values)
        {
            foreach (var marking in layerMarkings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                displacements.TryGetValue(proto.BodyPart, out var displacement);
                for (var i = 0; i < proto.Sprites.Count; i++)
                {
                    DebugTools.Assert(proto.Sprites[i] is SpriteSpecifier.Rsi, $"Marking {proto.ID} has a non-RSI sprite");

                    var index = _sprite.AddLayer(sprite.AsNullable(), proto.Sprites[i]);
                    var layerId = TrackLayer(ent, sprite, index);

                    var color = marking.MarkingColors is { } colors && i < colors.Count ? colors[i] : Color.White;
                    _sprite.LayerSetColor(sprite.AsNullable(), index, color);

                    if (displacement != null && proto.CanBeDisplaced)
                        _displacement.TryAddDisplacement(displacement, sprite, index, layerId, out _);
                }
            }
        }
    }
}
