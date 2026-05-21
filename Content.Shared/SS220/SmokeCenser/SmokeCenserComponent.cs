// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos;
using Robust.Shared.Audio;
namespace Content.Shared.SS220.SmokeCenser;

[RegisterComponent]
public sealed partial class CenserComponent : Component
{
    [DataField]
    public Gas GasType = Gas.WaterVapor;

    [DataField]
    public float VaporAmount = 5.0f;

    [DataField]
    public float Moles = 100f;

    [DataField]
    public float Temperature = 350f;

    [DataField]
    public SoundSpecifier? SoundUse;
}