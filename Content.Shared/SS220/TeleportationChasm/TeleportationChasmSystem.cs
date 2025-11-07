// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Labels.Components;
using Content.Shared.Movement.Events;
using Content.Shared.SS220.Photocopier.Forms;
using Content.Shared.Station.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Storage;
using Content.Shared.Tiles;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.TeleportationChasm;

/// <summary>
///     Handles making entities fall into chasms when stepped on.
/// </summary>
public sealed class TeleportationChasmSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGrapplingGunSystem _grapple = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportationChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<TeleportationChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<TeleportationChasmFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // don't predict queuedels on client
        if (_net.IsClient)
            return;

        List<EntityUid> toRemove = [];

        var query = EntityQueryEnumerator<TeleportationChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasm))
        {
            if (_timing.CurTime < chasm.NextDeletionTime)
                continue;

            TeleportToRandomLocation(uid);

            toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            RemComp<TeleportationChasmFallingComponent>(uid);
        }
    }

    private void OnStepTriggered(Entity<TeleportationChasmComponent> ent, ref StepTriggeredOffEvent args)
    {
        // already doomed
        if (HasComp<TeleportationChasmFallingComponent>(args.Tripper))
            return;

        StartFalling(ent, args.Tripper);
    }

    public void StartFalling(Entity<TeleportationChasmComponent> ent, EntityUid tripper, bool playSound = true)
    {
        var falling = AddComp<TeleportationChasmFallingComponent>(tripper);

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(tripper);

        if (playSound)
            _audio.PlayPredicted(ent.Comp.FallingSound, ent, tripper);
    }

    private void OnStepTriggerAttempt(Entity<TeleportationChasmComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (_grapple.IsEntityHooked(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }

    private void OnUpdateCanMove(Entity<TeleportationChasmFallingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void TeleportToRandomLocation(EntityUid ent)
    {
        var validLocations = new List<EntityCoordinates>();

        //var locations = EntityQueryEnumerator<FloorTileComponent, TransformComponent>();//I think only objects has tranform, so i mybe shoul pick smth else
        //while (locations.MoveNext(out _, out _, out var transform))
        //{
        //    //ToDo_SS220 figure out station check
        //    validLocations.Add(transform.Coordinates);
        //}

        var locations = EntityQueryEnumerator<FloorTileComponent>();//smth else
        while (locations.MoveNext(out _, out _))
        {
            if (!TryComp<TransformComponent>(ent, out var transform))
                continue;

            validLocations.Add(transform.Coordinates);
        }

        var teleportLocation = _random.Pick(validLocations);

        var xform = Transform(ent);
        _transformSystem.SetCoordinates(ent, xform, teleportLocation);
    }
}
