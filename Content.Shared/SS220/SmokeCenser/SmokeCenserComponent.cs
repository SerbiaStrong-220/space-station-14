// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Content.Shared.FixedPoint;
namespace Content.Shared.SS220.SmokeCenser;

[RegisterComponent]
public sealed partial class CenserComponent : Component
{
    [DataField]
    public Gas GasType = Gas.WaterVapor;

    [DataField]
    public FixedPoint2 WaterCost = 5.0;

    [DataField]
    public float Moles = 20f;

    [DataField]
    public float Temperature = 350f;

    [DataField]
    public SoundSpecifier? SoundUse;
}