// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mobs;
using Content.Shared.Stealth.Components;
using Content.Client.Stealth;

namespace Content.Client.SS220.Overlays;

public abstract class IgnoreLightVisionOverlay : Overlay
{
    [Dependency] protected readonly IEntityManager Entity = default!;
    [Dependency] protected readonly IPlayerManager PlayerManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    /// <summary> Defines radius in which you can see entities in containers </summary>
    protected float ShowCloseRadius;
    protected float ShowRadius;

    private readonly ContainerSystem _container;
    private readonly EntityLookupSystem _entityLookup;
    private readonly StealthSystem _stealthSystem;
    /// <summary> If use lesser value wierd thing happens with admin spawn menu and GetEntitiesInRange. </summary>
    private const float MIN_CLOSE_RANGE = 1.5f;
    /// <summary>Useless const due to how stealth work, but if they change it...</summary>
    private const float STEALTH_VISION_TRESHHOLD = -0.3f;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public IgnoreLightVisionOverlay(float showRadius, float closeShowRadius)
    {
        IoCManager.InjectDependencies(this);

        _container = Entity.System<ContainerSystem>();
        _entityLookup = Entity.System<EntityLookupSystem>();
        _stealthSystem = Entity.System<StealthSystem>();

        ShowRadius = showRadius < MIN_CLOSE_RANGE ? MIN_CLOSE_RANGE : showRadius;
        ShowCloseRadius = closeShowRadius < MIN_CLOSE_RANGE ? MIN_CLOSE_RANGE : closeShowRadius;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (PlayerManager.LocalEntity == null)
            return;
        if (!Entity.TryGetComponent<MobStateComponent>(PlayerManager.LocalEntity, out var mobstateComp))
            return;
        if (mobstateComp.CurrentState != MobState.Alive)
            return;
        if (!Entity.TryGetComponent<TransformComponent>(PlayerManager.LocalEntity, out var playerTransform))
            return;

        var handle = args.WorldHandle;
        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;

        var entities = _entityLookup.GetEntitiesInRange<MobStateComponent>(playerTransform.Coordinates, ShowRadius);
        var entitiesClose = _entityLookup.GetEntitiesInRange<MobStateComponent>(playerTransform.Coordinates, ShowCloseRadius);

        foreach (var (uid, stateComp) in entities)
        {
            var isCloseToOwner = entitiesClose.Contains((uid, stateComp));

            if (CantBeRendered(uid, out var sprite, out var xform))
                continue;
            if (CantBeSeen((uid, stateComp)))
                continue;
            if (IsStealthEnough(uid, isCloseToOwner))
                continue;
            if (_container.IsEntityOrParentInContainer(uid))
                if (CantBeVisibleInContainer(uid, isCloseToOwner))
                    continue;

            Render((uid, sprite, xform), eye?.Position.MapId, handle, eyeRot);
        }
        handle.SetTransform(Matrix3x2.Identity);
    }
    protected abstract void Render(Entity<SpriteComponent, TransformComponent> ent,
                        MapId? map, DrawingHandleWorld handle, Angle eyeRot);
    /// <summary>
    ///  function which defines what entities can be seen, f.e. pai or human, bread dog or reaper
    ///  Also contains list of components which defines it
    /// </summary>
    /// <returns> True if entities could be seen by thermals. Without any other obstacles </returns>
    private bool CantBeSeen(Entity<MobStateComponent> target)
    {
        var states = target.Comp.AllowedStates;

        if (target.Comp.CurrentState == MobState.Dead)
            return true;

        if (states.Contains(MobState.Dead) &&
            states.Contains(MobState.Alive))
            return false;

        return true;
    }
    private bool CantBeRendered(EntityUid target, [NotNullWhen(false)] out SpriteComponent? sprite,
                                                [NotNullWhen(false)] out TransformComponent? xform)
    {
        sprite = null;
        xform = null;

        if (!Entity.TryGetComponent<SpriteComponent>(target, out sprite))
            return true;
        if (!Entity.TryGetComponent<TransformComponent>(target, out xform))
            return true;

        return false;
    }
    /// <summary>
    ///  function which defines what entities visible or not.
    ///  Also contains const values of invis perception
    /// </summary>
    /// <returns>True if entities could be seen by thermals. Without any other obstacles </returns>
    private bool IsStealthEnough(EntityUid target, bool isCloseToOwner)
    {
        if (!Entity.TryGetComponent<StealthComponent>(target, out var component))
            return false;

        if (!isCloseToOwner &&
                _stealthSystem.GetVisibility(target, component) > STEALTH_VISION_TRESHHOLD)
            return true;

        return false;
    }
    /// <summary> function for verifying if we can see smth in container </summary>
    /// <returns>True if entities could be seen by thermals. Without any other obstacles </returns>
    private bool CantBeVisibleInContainer(EntityUid target, bool isCloseToOwner)
    {
        var blacklistComponentNames = new List<string>() { "DarkReaper", "Devourer" };

        if (isCloseToOwner == false)
            return true;

        var currentEntUid = target;
        while (_container.TryGetContainingContainer((currentEntUid, null, null), out var container))
        {
            currentEntUid = container.Owner;

            if (currentEntUid == PlayerManager.LocalEntity)
                return true;
            if (HasComponentFromList(currentEntUid, blacklistComponentNames))
                return true;
        }

        return false;
    }
    /// <summary> Checks if entity has a components from list </summary>
    /// <returns> True if entity has any of the listed components </returns>
    /// <exception cref="Exception"> Throw exception if List contains false comp name</exception>
    protected bool HasComponentFromList(EntityUid target, List<string> blacklistComponentNames)
    {
        foreach (var compName in blacklistComponentNames)
        {
            if (!_componentFactory.TryGetRegistration(compName, out var compReg))
                throw new Exception($"Cant find registration for component {compName} in blacklistComponents");

            if (Entity.HasComponent(target, compReg.Type))
                return true;
        }
        return false;
    }

}
