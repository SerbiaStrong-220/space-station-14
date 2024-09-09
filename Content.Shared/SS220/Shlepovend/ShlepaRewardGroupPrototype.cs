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
    public SponsorTier? RequiredRole = null;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string Name = "";

    /// <summary>
    /// Checks whether you need an exact role, not any tier higher than required.
    /// Use this for stuff like developer rewards.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public bool IsExactRoleRequired = false;

    /// <summary>
    /// Is this group hidden from shlepovend when the requirement is not met.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public bool IsHiddenOnInsufficient = false;
}
