// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Teleport.Components;

/// <summary>
///     Applies status effects from the Dictionary to the target after teleporting
/// </summary>
[RegisterComponent]
public sealed partial class AfterTeleportStatusEffectComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, TimeSpan> EffectsList;
}
