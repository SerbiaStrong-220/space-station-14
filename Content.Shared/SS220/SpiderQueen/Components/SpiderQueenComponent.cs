// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderQueenComponent : Component
{
    /// <summary>
    /// Current amount of mana
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 CurrentMana = FixedPoint2.Zero;

    /// <summary>
    /// Maximum amount of mana
    /// </summary>
    [DataField]
    public FixedPoint2 MaxMana = FixedPoint2.Zero;

    [DataField]
    public bool ShouldShowMana = false;

    /// <summary>
    /// How much mana will be generated in a second
    /// </summary>
    [DataField]
    public FixedPoint2 PassiveGeneration = 0.5f;

    [ViewVariables]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// List of actions
    /// </summary>
    [DataField]
    public List<EntProtoId>? Actions;

    /// <summary>
    /// Id of the cocoon prototype
    /// </summary>
    [DataField]
    public EntProtoId CocoonProto = "SpiderCocoon";
}
