// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.Smoking;

[RegisterComponent]
public sealed partial class SmokablePuffComponent : Component
{
    [DataField]
    public FixedPoint2 PuffCost = 15;

    [DataField]
    public Gas GasType = Gas.WaterVapor;

    [DataField]
    public float Moles = 4f;

    [DataField]
    public float Temperature = 350f;

    [DataField]
    public TimeSpan PuffDelay = TimeSpan.FromSeconds(1.2);

    [DataField]
    public SoundSpecifier? PuffInhaleSound = new SoundPathSpecifier("/Audio/SS220/Effects/puff.ogg");

    [DataField]
    public TimeSpan PuffCooldown = TimeSpan.FromSeconds(5);

    public TimeSpan NextPuffTime = TimeSpan.Zero;
}
