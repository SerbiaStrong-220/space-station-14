// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Teleport.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class DeleteTeleporterOnUseSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeleteTeleporterOnUseComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnTargetTeleported(Entity<DeleteTeleporterOnUseComponent> ent, ref TargetTeleportedEvent args)
    {
        ent.Comp.RemainingUses--;

        if (ent.Comp.RemainingUses > 0)
            return;

        _audio.PlayPredicted(ent.Comp.DeleteSound, ent, args.Target);

        PredictedQueueDel(ent);
    }
}
