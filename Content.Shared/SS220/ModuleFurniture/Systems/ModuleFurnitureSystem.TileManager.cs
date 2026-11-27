// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.ModuleFurniture.Systems;

public abstract partial class SharedModuleFurnitureSystem<T> : EntitySystem
{
    public virtual void PrintDebugOccupation(SharedModuleFurnitureComponent furnitureComp)
    {
        // Server only realization to have a proper ability to debug
    }

    /// <summary>
    /// Gets all right border of filled part of the layout in the <paramref name="entity"/>
    /// </summary>
    protected List<Vector2i> GetOccupiedTilesBorders(Entity<SharedModuleFurnitureComponent> furniture)
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

    protected bool TryGetOffsetForPlacement(SharedModuleFurnitureComponent furnitureComp,
                                ModuleFurniturePartComponent partComp, [NotNullWhen(true)] out Vector2i? offset)
    {
        // It should finds best suitable place:
        // It should work predictable and player-friendly for making their own furniture
        // So steps: 0. checks if we can be placed righter to the last places part.
        // 2. If can be placed righter to last -> tries to move higher until we cant be placed.
        // 3. If cant be placed righter to last -> tries to move lower until we cant be placed.

        var partSize = furnitureComp.CachedLayout.Count == 0
                        ? Vector2i.Zero
                        : Comp<ModuleFurniturePartComponent>(GetEntity(furnitureComp.CachedLayout.Values.Last())).ContainerSize;

        var predictOffset = furnitureComp.CachedLayout.Count == 0 ?
                            Vector2i.Zero :
                            furnitureComp.CachedLayout.Keys.Last() + (partSize.X, 0); //Slide right to occupied point

        if (CanBePlaced(furnitureComp, partComp, predictOffset))
        {
            while (CanBePlaced(furnitureComp, partComp, predictOffset + Vector2i.Down)) // down - (0, -1) which is actually up in our case
            {
                predictOffset += Vector2i.Down; // dont forget about it
            }
        }
        else if (CanBePlaced(furnitureComp, partComp, (0, (predictOffset + Vector2i.Up).Y))) // up - (0, 1) which is actually down in our case
        {
            predictOffset = (0, (predictOffset + Vector2i.Up).Y); // and about it
        }
        else
        {
            offset = null;
            return false;
        }

        offset = predictOffset;
        return true;
    }

    private bool CanBePlaced(SharedModuleFurnitureComponent furnitureComp,
                                ModuleFurniturePartComponent partComp, Vector2i placeVector)
    {
        var partSize = partComp.ContainerSize;

        if (placeVector.X < Vector2i.Zero.X || placeVector.Y < Vector2i.Zero.Y
        || !((placeVector + partSize).X - 1 < furnitureComp.TileLayoutSize.X) // well its designed skill issue. I operate 1x1 cells and vector is actually a cell
        || !((placeVector + partSize).Y - 1 < furnitureComp.TileLayoutSize.Y)) // same here
            return false;

        for (var height = 0; height < partSize.Y; height++)
        {
            for (int width = 0; width < partSize.X; width++)
            {
                var keyVector = placeVector + (width, height);
                if (furnitureComp.CachedOccupation[keyVector])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
