// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Actions;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.Shitspawn.AshDrake;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeRoarSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private static readonly AudioParams RoarAudio = AudioParams.Default.WithVolume(8f).WithMaxDistance(20f);
    private static readonly EntProtoId ActionAshDrakeRoar = "ActionAshDrakeRoar";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeRoarComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AshDrakeRoarComponent, AshDrakeRoarActionEvent>(OnRoar);
    }

    private void OnMapInit(EntityUid uid, AshDrakeRoarComponent comp, MapInitEvent args)
    {
        _actions.AddAction(uid, ref comp.ActionEntity, ActionAshDrakeRoar);
    }

    private void OnRoar(EntityUid uid, AshDrakeRoarComponent comp, AshDrakeRoarActionEvent args)
    {
        args.Handled = true;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/SS220/Shitspawn/AshDrake/drake_roar.ogg"), uid, RoarAudio);
        Spawn("AshDrakeRoarEffect", Transform(uid).Coordinates);

        var entities = _lookup.GetEntitiesInRange(uid, comp.Radius);
        foreach (var target in entities)
        {
            if (target == uid)
                continue;

            if (_faction.IsEntityFriendly(uid, target))
                continue;

            _stun.TryKnockdown(target, TimeSpan.FromSeconds(comp.StunDuration), true);
        }
    }
}
