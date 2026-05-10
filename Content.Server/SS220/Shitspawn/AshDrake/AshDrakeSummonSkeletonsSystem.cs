// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.SS220.Shitspawn.AshDrake;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeSummonSkeletonsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const string SkeletonProto = "MobAshDrakeSkeleton";
    private const string BoneSound = "/Audio/Effects/bone_rattle.ogg";
    private const string SpawnSound = "/Audio/SS220/Shitspawn/AshDrake/skeleton_spawn.ogg";
    private static readonly AudioParams BoneAudio = AudioParams.Default.WithVolume(6f);
    private static readonly AudioParams SpawnAudio = AudioParams.Default.WithVolume(8f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeSummonSkeletonsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AshDrakeSummonSkeletonsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AshDrakeSummonSkeletonsComponent, AshDrakeSummonSkeletonsActionEvent>(OnSummon);
        SubscribeLocalEvent<AshDrakeSkeletonComponent, MobStateChangedEvent>(OnSkeletonDeath);
    }

    private void OnMapInit(EntityUid uid, AshDrakeSummonSkeletonsComponent comp, MapInitEvent args)
        => _actions.AddAction(uid, ref comp.ActionEntity, comp.ActionId);

    private void OnShutdown(EntityUid uid, AshDrakeSummonSkeletonsComponent comp, ComponentShutdown args)
        => _actions.RemoveAction(uid, comp.ActionEntity);

    private void OnSummon(EntityUid uid, AshDrakeSummonSkeletonsComponent comp, AshDrakeSummonSkeletonsActionEvent args)
    {
        args.Handled = true;

        var xform = Transform(uid);
        var origin = _transform.GetWorldPosition(xform);
        var mapId = xform.MapID;

        _audio.PlayPvs(new SoundPathSpecifier(SpawnSound), uid, SpawnAudio);

        for (var i = 0; i < comp.Count; i++)
        {
            var angle = MathF.Tau * i / comp.Count;
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * comp.SpawnRadius;
            var skeleton = Spawn(SkeletonProto, new MapCoordinates(origin + offset, mapId));
            _audio.PlayPvs(new SoundPathSpecifier(BoneSound), skeleton, BoneAudio);
        }
    }

    private void OnSkeletonDeath(EntityUid uid, AshDrakeSkeletonComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        _audio.PlayPvs(new SoundPathSpecifier(BoneSound), Transform(uid).Coordinates, BoneAudio);
        QueueDel(uid);
    }
}
