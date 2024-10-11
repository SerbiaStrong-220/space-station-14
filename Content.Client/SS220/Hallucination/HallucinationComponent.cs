// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Hallucination;

namespace Content.Client.SS220.Hallucination;
/// <summary>
/// lmaoooooo??
/// </summary>
[RegisterComponent]
public sealed partial class HallucinationComponent : SharedHallucinationComponent
{
    // yeah yeah make it to client only
    /// <summary>
    /// used only if it is localEntity of the client
    /// </summary>
    public List<TimeSpan> HallucinationSpawnerTimers = [];
}
