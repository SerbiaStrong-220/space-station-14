using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Server.SS220.LiquidSplatters;

[DataDefinition]
public sealed partial class CleanSplattersTileReaction : ITileReaction
{
    public FixedPoint2 TileReact(TileRef tile,
        ReagentPrototype reagent,
        FixedPoint2 reactVolume,
        IEntityManager entityManager,
        List<ReagentData>? data)
    {
        return entityManager.System<LiquidSplattersSystem>().CleanSplattersOnTile(tile, reagent, reactVolume);
    }
}
