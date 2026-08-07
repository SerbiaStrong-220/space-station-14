// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;

/// <summary>
/// Reagents that supress a strain. For an RNA any one of them.
/// For a DNA strain and uperviruses needs all of them present at once.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class VirusCure
{
    [DataField]
    public List<ProtoId<ReagentPrototype>> Reagents = [];

    public VirusCure Clone() => new() { Reagents = [.. Reagents] };
}
