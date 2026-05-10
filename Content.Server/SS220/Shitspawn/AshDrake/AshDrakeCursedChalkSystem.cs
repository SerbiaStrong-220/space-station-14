// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DoAfter;
using Content.Server.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Shitspawn.AshDrake;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeCursedChalkSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    private static readonly AudioParams DrawAudio = AudioParams.Default.WithVolume(4f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeCursedChalkComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<AshDrakeCursedChalkComponent, AshDrakeCursedChalkDoAfterEvent>(OnDoAfter);
    }

    private void OnInteract(EntityUid uid, AshDrakeCursedChalkComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target != null)
            return;

        if (!args.CanReach)
        {
            _popup.PopupEntity(Loc.GetString("ash-drake-chalk-too-far"), uid, args.User);
            return;
        }

        if (!args.ClickLocation.IsValid(EntityManager))
            return;

        args.Handled = true;

        var netCoords = GetNetCoordinates(args.ClickLocation);
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.CastTime,
            new AshDrakeCursedChalkDoAfterEvent(netCoords), uid, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, AshDrakeCursedChalkComponent comp, AshDrakeCursedChalkDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var coords = GetCoordinates(args.ClickLocation);
        if (!coords.IsValid(EntityManager))
            return;

        Spawn(comp.RuneProto, coords);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/SS220/Shitspawn/AshDrake/skeleton_spawn.ogg"), args.User, DrawAudio);
        _popup.PopupCoordinates(Loc.GetString("ash-drake-chalk-placed"), coords, args.User, PopupType.Medium);
        QueueDel(uid);
    }
}
