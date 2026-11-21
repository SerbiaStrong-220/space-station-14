// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Electrocution;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Shared.Jittering;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NodeContainer;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.NPC.CableGnaw;

/// <summary>
/// Handles mice (or any NPC) gnawing exposed cables and getting electrocuted to death.
/// </summary>
/// <remarks>
/// The actual electrocution is deferred to <see cref="Update"/>: killing an NPC from inside an HTN operator
/// update clears its plan mid-iteration and crashes the HTN system.
/// </remarks>
public sealed class CableGnawSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerNetSystem _powerNet = default!;

    private const string SparksPrototype = "EffectSparks";
    private const float GnawRange = 1.5f;
    private const int ShockDamage = 50;
    private static readonly TimeSpan ShockTime = TimeSpan.FromSeconds(4);

    private readonly List<(EntityUid Gnawer, EntityUid Cable)> _pendingGnaws = new();
    private readonly HashSet<EntityUid> _pendingGnawers = new();
    private readonly HashSet<EntityUid> _pendingCables = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingGnaws.Count == 0)
            return;

        // Copy so anything raised during electrocution can't mutate the list we iterate.
        var gnaws = _pendingGnaws.ToArray();
        _pendingGnaws.Clear();
        _pendingGnawers.Clear();
        _pendingCables.Clear();

        foreach (var (gnawer, cable) in gnaws)
        {
            ApplyGnaw(gnawer, cable);
        }
    }

    /// <summary>
    /// Returns true if the cooldown has passed. A freshly seen NPC gets an initial random cooldown,
    /// so newly spawned mice don't run for cables immediately.
    /// </summary>
    public bool IsOffCooldown(EntityUid uid, TimeSpan minCooldown, TimeSpan maxCooldown)
    {
        if (!TryComp<CableGnawCooldownComponent>(uid, out var comp))
        {
            comp = AddComp<CableGnawCooldownComponent>(uid);
            comp.NextAttempt = _timing.CurTime + RandomCooldown(minCooldown, maxCooldown);
            return false;
        }

        return _timing.CurTime >= comp.NextAttempt;
    }

    /// <summary>
    /// Consumes an attempt (restarts the cooldown) and rolls the chance.
    /// </summary>
    public bool RollAttempt(EntityUid uid, float chance, TimeSpan minCooldown, TimeSpan maxCooldown)
    {
        var comp = EnsureComp<CableGnawCooldownComponent>(uid);
        comp.NextAttempt = _timing.CurTime + RandomCooldown(minCooldown, maxCooldown);
        return _random.Prob(Math.Clamp(chance, 0f, 1f));
    }

    /// <summary>
    /// A cable is exposed if it's anchored, lies on a subfloor tile (no floor panel)
    /// and there is no wall / other impassable anchored structure on the same tile.
    /// </summary>
    public bool IsCableExposed(EntityUid cable)
    {
        if (TerminatingOrDeleted(cable) || _pendingCables.Contains(cable) || _container.IsEntityInContainer(cable))
            return false;

        if (!TryComp(cable, out TransformComponent? xform) || !xform.Anchored)
            return false;

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var indices = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var tileRef = _map.GetTileRef(gridUid, grid, indices);

        if (tileRef.Tile.IsEmpty)
            return false;

        // Covered by floor panels.
        if (!_turf.GetContentTileDefinition(tileRef).IsSubFloor)
            return false;

        // Under a wall (or any other hard impassable anchored structure).
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, indices);
        while (anchored.MoveNext(out var uid))
        {
            if (uid == null || uid.Value == cable)
                continue;

            if (!TryComp<PhysicsComponent>(uid.Value, out var body))
                continue;

            if (body.CanCollide && body.Hard && (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates and schedules a gnaw. The gnawer is electrocuted on the next system update.
    /// Safe to call from HTN operators.
    /// </summary>
    public bool TryGnaw(EntityUid gnawer, EntityUid cable)
    {
        if (TerminatingOrDeleted(gnawer) || _mobState.IsIncapacitated(gnawer))
            return false;

        if (_pendingGnawers.Contains(gnawer))
            return true;

        if (!IsCableExposed(cable) || !InGnawRange(gnawer, cable))
            return false;

        _pendingGnaws.Add((gnawer, cable));
        _pendingGnawers.Add(gnawer);
        _pendingCables.Add(cable);
        return true;
    }

    private void ApplyGnaw(EntityUid gnawer, EntityUid cable)
    {
        if (TerminatingOrDeleted(gnawer) || TerminatingOrDeleted(cable))
            return;

        if (_mobState.IsIncapacitated(gnawer) || !IsCableExposed(cable) || !InGnawRange(gnawer, cable))
            return;

        var cableCoords = Transform(cable).Coordinates;

        if (HasPower(cable))
        {
            _popup.PopupEntity(Loc.GetString("mouse-gnaw-cable-popup", ("mouse", gnawer)), gnawer, PopupType.MediumCaution);
            Spawn(SparksPrototype, cableCoords);

            _electrocution.TryDoElectrocution(
                gnawer,
                cable,
                ShockDamage,
                ShockTime,
                refresh: true,
                siemensCoefficient: 1f,
                ignoreInsulation: true);

            if (!TerminatingOrDeleted(gnawer))
            {
                _jitter.DoJitter(gnawer, ShockTime * 0.75, refresh: true, amplitude: 80f, frequency: 8f);

                // Guarantee death so a single mouse can't chew through the whole station.
                if (!_mobState.IsDead(gnawer))
                    _mobState.ChangeMobState(gnawer, MobState.Dead);
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("mouse-gnaw-cable-safe-popup", ("mouse", gnawer)), gnawer, PopupType.Small);
        }

        QueueDel(cable);
    }

    private bool HasPower(EntityUid cable)
    {
        if (!TryComp<NodeContainerComponent>(cable, out var nodeContainer))
            return false;

        foreach (var node in nodeContainer.Nodes.Values)
        {
            if (node.NodeGroup is not IBasePowerNet powerNet)
                continue;

            var stats = _powerNet.GetNetworkStatistics(powerNet.NetworkNode);
            if (stats.SupplyCurrent > 0f)
                return true;
        }

        return false;
    }

    private bool InGnawRange(EntityUid gnawer, EntityUid cable)
    {
        return Transform(gnawer).Coordinates.TryDistance(EntityManager, _transform, Transform(cable).Coordinates, out var distance)
               && distance <= GnawRange;
    }

    private TimeSpan RandomCooldown(TimeSpan min, TimeSpan max)
    {
        if (max < min)
            (min, max) = (max, min);

        return min + (max - min) * _random.NextDouble();
    }
}
