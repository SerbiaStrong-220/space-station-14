// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.SuperMatterCrystal;
// TODO: Add PointLight variety depends on SM state or anything else
public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
}
