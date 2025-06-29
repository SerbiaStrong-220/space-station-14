// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.ElectricalChair;

[RegisterComponent, Access(typeof(ElectricalChairSystem))]
public sealed partial class ElectricalChairComponent : Component
{
    [ViewVariables]
    public TimeSpan NextDamageSecond = TimeSpan.Zero;

    [DataField]
    public bool Enabled = false;

    [DataField]
    public int DamagePerSecond = 40;

    [DataField]
    public int ElectrocuteTime = 4;

    [DataField]
    public bool PlaySoundOnShock = true;

    [DataField]
    public SoundSpecifier ShockNoises = new SoundCollectionSpecifier("sparks");

    [DataField]
    public float ShockVolume = 20;

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";
}
