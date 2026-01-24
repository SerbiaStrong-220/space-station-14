// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ResourceMiner;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ResourceMinerComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<ProtoId<MaterialPrototype>, int> GenerationAmount = new();

    [DataField]
    [AutoNetworkedField]
    public EntityUid? Silo;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan NextUpdate;

    [DataField]
    [AutoNetworkedField]
    [AlwaysPushInheritance]
    public TimeSpan TimeBetweenUpdate = TimeSpan.FromSeconds(1f);

    [DataField]
    [AutoNetworkedField]
    [AlwaysPushInheritance]
    public Color WorkingColor = Color.FromHex("#228B22");

    [DataField]
    [AutoNetworkedField]
    [AlwaysPushInheritance]
    public Color ErrorColor = Color.FromHex("#E71616");

    [DataField]
    [AutoNetworkedField]
    [AlwaysPushInheritance]
    public SoundSpecifier WorkSound = new SoundPathSpecifier("/Audio/Ambience/Objects/engine_hum.ogg")
    {
        Params = AudioParams.Default.WithVariation(1.15f).WithVolume(-3f)
    };

    [DataField]
    public string TurnOnState = "turned_on";
}
