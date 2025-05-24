// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Clothing;
using Content.Shared.SS220.StuckOnEquip;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Clothing;

/// <summary>
/// </summary>
public sealed class InnerHandToggleableSystem : SharedInnerHandToggleableSystem
{
    /// <summary>
    /// Prefix for any inner hand.
    /// </summary>
    public const string InnerHandPrefix = "inner_";

    public override void Initialize()
    {
        base.Initialize();
    }
 
}
