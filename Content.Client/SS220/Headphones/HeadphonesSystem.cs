using System.IO.Pipes;
using System.Threading.Tasks.Dataflow;
using Content.Shared.Audio.Jukebox;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.SS220.Headphones;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Headphones;

public sealed class HeadphonesSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(SharedAudioSystem));

        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<HeadphonesComponent, JukeboxComponent>();

        while (query.MoveNext(out var uid, out var headphones, out var jukebox))
        {
            if (jukebox.AudioStream == null)
                continue;

            var player = _playerManager.LocalEntity;

            if (player == null)
                continue;

            var parent = Transform(uid).ParentUid;

            if (parent == player)
                continue;

            if (!TryComp<AudioComponent>(jukebox.AudioStream, out var audio))
                continue;

            _audio.SetGain(jukebox.AudioStream, headphones.VolumeModificator, audio);

        }

    }

}
