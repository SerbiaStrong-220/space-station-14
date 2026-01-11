// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.SS220.Surgery.Components;

/// <summary>
/// Marks item that can scan patient body on surgeries
/// </summary>
[RegisterComponent]
public sealed partial class ImplantCheckInSurgeryComponent : Component
{
    [DataField]
    public EntProtoId OutputPaper = "ImplantCheckReportPaper";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg",
            AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));
}
