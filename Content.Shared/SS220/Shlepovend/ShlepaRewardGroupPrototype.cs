using Content.Shared.SS220.Discord;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Shlepovend;

[Prototype("ShlepaRewardGroup")]
public sealed class ShlepaRewardGroupPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public int RoundstartTokens = 0;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public Dictionary<EntProtoId, int> Rewards = new();

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public Enum? RequiredRole = null;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string Name = "";
}
