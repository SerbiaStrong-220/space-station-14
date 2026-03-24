// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameObjects;

namespace Content.Server.SS220.Shitspawn.AshDrake;

[RegisterComponent]
public sealed partial class AshDrakeGreatFireballLavaTrailComponent : Component
{
    public const float LavaTrailInterval = 0.2f;
    public float TimeSinceLastLava = LavaTrailInterval;
}
