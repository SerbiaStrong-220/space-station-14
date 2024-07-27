// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.SS220.Thermals;
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
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private readonly ContainerSystem _container;
    private readonly EntityLookupSystem _entityLookup;
    private readonly StealthSystem _stealthSystem;
    private readonly float _showRadius;
    private readonly float _showCloseRadius;
    private const float MIN_RANGE = 0.3f;
    /// <summary>Useless const due to how stealth work, but if they change it...</summary>
    private const float STEALTH_VISION_TRESHHOLD = 0;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public IgnoreLightVisionOverlay(float showRadius)
    {
        IoCManager.InjectDependencies(this);

        _container = Entity.System<ContainerSystem>();
        _entityLookup = Entity.System<EntityLookupSystem>();
        _stealthSystem = Entity.System<StealthSystem>();

        _showRadius = showRadius;
        _showCloseRadius = _showRadius / 4 < MIN_RANGE ? MIN_RANGE : _showRadius / 4;
    }
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity == null)
            return;
        if (!Entity.TryGetComponent<MobStateComponent>(_playerManager.LocalEntity, out var mobstateComp))
            return;
        if (mobstateComp.CurrentState != MobState.Alive)
            return;
        if (!Entity.TryGetComponent(_playerManager.LocalEntity, out ThermalVisionComponent? thermalVision) ||
            thermalVision.State == ThermalVisionState.Off)
            return;

        if (Entity.TryGetComponent<TransformComponent>(_playerManager.LocalEntity,
                                                out var playerTransform) == false)
            return; // maybe need to log it
        var handle = args.WorldHandle;
        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;

        if (_showRadius < MIN_RANGE)
            return; // can cause execp also need to log it

        var entities = _entityLookup.GetEntitiesInRange<MobStateComponent>(playerTransform.Coordinates, _showRadius);
        var entitiesClose = _entityLookup.GetEntitiesInRange<MobStateComponent>(playerTransform.Coordinates, _showCloseRadius);

        foreach (var (uid, stateComp) in entities)
        {
            var isCloseToOwner = entitiesClose.Contains((uid, stateComp));

            if (CantBeRendered(uid, out var sprite, out var xform))
                continue;
            if (CantBeSeenByThermals((uid, stateComp)))
                continue;
            if (IsInvisibleToThermals(uid, isCloseToOwner))
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
    ///  function wich defines what entities can be seen, f.e. pai or human, bread dog or reaper
    ///  Also contains list of components which defines it
    /// </summary>
    /// <returns>
    ///  True if entities could be seen by thermals. Without any other obstacles
    /// </returns>
    private bool CantBeSeenByThermals(Entity<MobStateComponent> target)
    {
        var states = target.Comp.AllowedStates;

        if (states.Contains(MobState.Dead) && states.Contains(MobState.Alive))
            return false;

        return true;
    }
    private bool CantBeRendered(EntityUid target, [NotNullWhen(false)] out SpriteComponent? sprite,
                                                [NotNullWhen(false)] out TransformComponent? xform)
    {
        sprite = null;
        xform = null;

        if (Entity.TryGetComponent<SpriteComponent>(target, out sprite) == false)
            return true;
        if (Entity.TryGetComponent<TransformComponent>(target, out xform) == false)
            return true;

        return false;
    }
    /// <summary>
    ///  function wich defines what entities visible or not.
    ///  Also contains const values of invis perception
    /// </summary>
    /// <returns>
    ///  True if entities could be seen by thermals. Without any other obstacles
    /// </returns>
    private bool IsInvisibleToThermals(EntityUid target, bool isCloseToOwner)
    {
        if (Entity.TryGetComponent<StealthComponent>(target, out var component))
            if (isCloseToOwner == false)
                if (_stealthSystem.GetVisibility(target, component) < STEALTH_VISION_TRESHHOLD)
                    return true;

        return false;
    }
    /// <summary>
    ///  function wich defines what entities visible or not.
    ///  Also contains const values of invis perception
    /// </summary>
    /// <returns>
    ///  True if entities could be seen by thermals. Without any other obstacles
    /// </returns>
    private bool CantBeVisibleInContainer(EntityUid target, bool isCloseToOwner)
    {
        var blacklistComponentNames = new List<string>() { "DarkReaper", "Devourer" };

        if (isCloseToOwner == false)
            return true;

        var currentEntUid = target;
        while (_container.TryGetContainingContainer((currentEntUid, null, null), out var container))
        {
            currentEntUid = container.Owner;

            if (HasComponentFromList(currentEntUid, blacklistComponentNames))
                return true;
        }

        return false;
    }
    /// <summary>
    /// Checks if entity has a components from list
    /// </summary>
    /// <param name="target"></param>
    /// <param name="blacklistComponentNames"></param>
    /// <returns>
    /// true if entity has any of the listed components
    /// </returns>
    /// <exception cref="Exception"> Throw excep if List contains false comp name</exception>
    private bool HasComponentFromList(EntityUid target, List<string> blacklistComponentNames)
    {
        foreach (var compName in blacklistComponentNames)
        {
            if (_componentFactory.TryGetRegistration(compName, out var compReg) == false)
                throw new Exception($"Cant find registration for component {compName} in blacklistComponents");

            if (Entity.HasComponent(target, compReg.Type))
                return true;
        }
        return false;
    }

}
