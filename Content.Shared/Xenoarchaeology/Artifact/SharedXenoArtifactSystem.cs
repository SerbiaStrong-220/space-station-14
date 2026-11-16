using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact;

/// <summary>
/// Handles all logic for generating and facilitating interactions with XenoArtifacts
/// </summary>
public abstract partial class SharedXenoArtifactSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!; // SS220-BonusForFullyDiscovered

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoArtifactComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<XenoArtifactComponent, ArtifactSelfActivateEvent>(OnSelfActivate);

        InitializeNode();
        InitializeUnlock();
        InitializeXAT();
        InitializeXAE();
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateUnlock(frameTime);
    }

    /// <summary> As all artifacts have to contain nodes - we ensure that they are containers. </summary>
    private void OnStartup(Entity<XenoArtifactComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ent.Comp.SelfActivateAction);
        ent.Comp.NodeContainer = _container.EnsureContainer<Container>(ent, XenoArtifactComponent.NodeContainerId);
    }

    private void OnSelfActivate(Entity<XenoArtifactComponent> ent, ref ArtifactSelfActivateEvent args)
    {
        args.Handled = TryActivateXenoArtifact(ent, ent, null, Transform(ent).Coordinates, false);
    }

    public void SetSuppressed(Entity<XenoArtifactComponent> ent, bool val)
    {
        if (ent.Comp.Suppressed == val)
            return;

        ent.Comp.Suppressed = val;
        Dirty(ent);
    }

    // SS220-BonusForFullyDiscovered - start
    private void CheckFullyDiscoveredBonus(Entity<XenoArtifactComponent> artifact)
    {
        if (!_net.IsServer)
            return;

        foreach (var segment in artifact.Comp.CachedSegments)
        {
            foreach (var netEnt in segment)
            {
                if (!TryComp<XenoArtifactNodeComponent>(GetEntity(netEnt), out var nodeComponent))
                    continue;

                // If at least 1 node is locked it does not issue a bonus.
                if (nodeComponent.Locked)
                    return;
            }
        }

        artifact.Comp.IsBonusIssued = true;
        SpawnBonus(artifact);
    }

    private void SpawnBonus(Entity<XenoArtifactComponent> artifact)
    {
        var spawns = _entityTable.GetSpawns(artifact.Comp.BonusTable);

        foreach (var proto in spawns)
        {
            if (!TrySpawnNextTo(proto, artifact, out var item))
                continue;

            var xform = Transform(item.Value);
            var throwing = xform.LocalRotation.ToWorldVec() * 5f; // magic number throwing force
            var direction = xform.Coordinates.Offset(throwing);

            _throwing.TryThrow(item.Value, direction);
        }
    }
    // SS220-BonusForFullyDiscovered - end
}
