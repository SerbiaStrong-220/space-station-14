// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Items;
using Content.Shared.SS220.Forensics;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Adds the selected collection mode to the held scanner's HUD.
/// </summary>
public sealed class GloveMicroFiberScannerStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<GloveMicroFiberScannerComponent>(ent => new GloveMicroFiberScannerStatusControl(ent));
    }
}
