// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;

namespace Content.Shared.SS220.ModuleFurniture.Systems;

public abstract partial class SharedModuleFurnitureSystem : EntitySystem
{
    /// <summary>
    /// Gets all right border of filled part of the layout in the <paramref name="entity"/>
    /// </summary>
    internal List<Vector2i> GetOccupiedTilesBorders(Entity<ModuleFurnitureComponent> furniture)
    {
        var result = new List<Vector2i>();

        for (var height = 0; height < furniture.Comp.TileLayoutSize.Y; height++)
        {
            // We actually seek for far occupied point, thats why we seek from width end cord
            var width = furniture.Comp.TileLayoutSize.X - 1;
            while (!furniture.Comp.CachedOccupation[(width, height)])
            {
                width--;
            }
            result.Add(new Vector2i(width, height));
        }

        return result;
    }
}
