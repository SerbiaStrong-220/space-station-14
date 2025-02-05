// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Systems;
using Robust.Server.Containers;
using Robust.Shared.Utility;
using System.Text;

namespace Content.Server.SS220.ModuleFurniture;

public sealed partial class ModuleFurnitureSystem : SharedModuleFurnitureSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;

    /// <summary>
    /// Honestly, never use this if you dont have a skill issue 😶‍🌫️
    /// </summary>
    private void ForceRebuildOccupation(Entity<ModuleFurnitureComponent> furniture)
    {
        foreach (var layoutEntry in furniture.Comp.CachedLayout)
        {
            var (key, value) = layoutEntry;
            var entrySize = Comp<ModuleFurniturePartComponent>(GetEntity(value)).ContainerSize;

            for (var height = 0; height < entrySize.Y; height++)
            {
                for (var width = 0; width < entrySize.X; width++)
                {
                    var occupationKey = key + (height, width);
                    DebugTools.Assert(!furniture.Comp.CachedOccupation[occupationKey]);
                    furniture.Comp.CachedOccupation[occupationKey] = true;
                }
            }
        }
    }

    private void PrintDebugOccupation(Entity<ModuleFurnitureComponent> furniture)
    {
        var builder = new StringBuilder($"Occupation of the {ToPrettyString(furniture)}").AppendLine();
        for (var height = 0; height < furniture.Comp.TileLayoutSize.Y; height++)
        {
            for (var width = 0; width < furniture.Comp.TileLayoutSize.X; width++)
            {
                if (furniture.Comp.CachedOccupation[(width, height)])
                    builder.Append('x');
                else
                    builder.Append('o');
            }
            builder.AppendLine();
        }
        Log.Debug(builder.ToString());
    }

    /// <summary>
    /// Force add part to the furniture. Errors when it cant be inserted to container of furniture.
    /// </summary>
    private void AddToModuleFurniture(Entity<ModuleFurnitureComponent> furniture, Entity<ModuleFurniturePartComponent> part, Vector2i offset)
    {
        DebugTools.Assert(!furniture.Comp.DrawerContainer.Contains(part));

        var partSize = part.Comp.ContainerSize;

        for (var height = 0; height < partSize.Y; height++)
        {
            for (var width = 0; width < partSize.X; width++)
            {
                var keyVector = offset + (width, height);
                DebugTools.Assert(!furniture.Comp.CachedOccupation[keyVector]);
                furniture.Comp.CachedOccupation[keyVector] = true;
            }
        }

        if (!_container.Insert(part.Owner, furniture.Comp.DrawerContainer))
        {
            Log.Error($"Error during inserting {ToPrettyString(part)} to {ToPrettyString(furniture)}");
        }
    }
}
