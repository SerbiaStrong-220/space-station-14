// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.GavelStand;

/// <summary>
/// Component that apply exclamation to people around when interacted with item from whitelist
/// </summary>
[RegisterComponent]
public sealed partial class GavelStandComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier? Sound;

    [DataField]
    public string Effect = "Exclamation";

    [DataField]
    public float Distance = 5;

    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();
}
