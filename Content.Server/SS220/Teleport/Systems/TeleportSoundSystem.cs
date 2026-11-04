// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.Teleport.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.Teleport.Systems;

public sealed partial class TeleportSoundSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportSoundComponent, BeforeTeleportEvent>(OnBeforeTeleport);
        SubscribeLocalEvent<TeleportSoundComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnBeforeTeleport(Entity<TeleportSoundComponent> ent, ref BeforeTeleportEvent args)
    {
        if (ent.Comp.DepartureSound is not { } sound)
            return;

        // Keep the sound at the departure location instead of letting it follow the teleported target.
        _audio.PlayPvs(sound, Transform(args.Target).Coordinates);
    }

    private void OnTargetTeleported(Entity<TeleportSoundComponent> ent, ref TargetTeleportedEvent args)
    {
        if (ent.Comp.ArrivalSound is not { } sound)
            return;

        // Keep the sound at the arrival location if the target moves away after teleporting.
        _audio.PlayPvs(sound, Transform(args.Target).Coordinates);
    }
}
