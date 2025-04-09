// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Systems;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Content.Server.SS220.ModuleFurniture;

public sealed partial class ModuleFurnitureSystem : SharedModuleFurnitureSystem<ModuleFurnitureComponent>
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void PrintDebugOccupation(SharedModuleFurnitureComponent furnitureComp)
    {
        var builder = new StringBuilder($"Occupation of the furniture").AppendLine();
        for (var height = 0; height < furnitureComp.TileLayoutSize.Y; height++)
        {
            for (var width = 0; width < furnitureComp.TileLayoutSize.X; width++)
            {
                if (furnitureComp.CachedOccupation[(width, height)])
                    builder.Append('x');
                else
                    builder.Append('o');
            }
            builder.AppendLine();
        }
        Log.Debug(builder.ToString());
    }

    /// <summary>
    /// Honestly, never use this if you dont have a skill issue 😶‍🌫️
    /// </summary>
    private void ForceRebuildOccupation(ModuleFurnitureComponent furnitureComp)
    {
        MakeClearOccupation(furnitureComp);

        foreach (var layoutEntry in furnitureComp.CachedLayout)
        {
            var (key, value) = layoutEntry;
            var entrySize = Comp<ModuleFurniturePartComponent>(GetEntity(value)).ContainerSize;

            for (var height = 0; height < entrySize.Y; height++)
            {
                for (var width = 0; width < entrySize.X; width++)
                {
                    var occupationKey = key + (height, width);
                    DebugTools.Assert(!furnitureComp.CachedOccupation[occupationKey]);
                    furnitureComp.CachedOccupation[occupationKey] = true;
                }
            }
        }
    }

    private void MakeClearOccupation(ModuleFurnitureComponent furnitureComp)
    {
        furnitureComp.CachedOccupation.Clear();
        for (var height = 0; height < furnitureComp.TileLayoutSize.Y; height++)
        {
            for (var width = 0; width < furnitureComp.TileLayoutSize.X; width++)
            {
                furnitureComp.CachedOccupation.Add((width, height), false);
            }
        }
    }

    /// <summary>
    /// Force add part to the furniture. Errors when it cant be inserted to container of furniture.
    /// </summary>
    private void AddToModuleFurniture(ModuleFurnitureComponent furnitureComp, Entity<ModuleFurniturePartComponent> part, Vector2i offset)
    {
        DebugTools.Assert(!furnitureComp.DrawerContainer.Contains(part));

        AddToOccupation(furnitureComp, part, offset);
#if DEBUG
        PrintDebugOccupation(furnitureComp);
#endif
        if (!_container.Insert(part.Owner, furnitureComp.DrawerContainer))
        {
            Log.Error($"Error during inserting {ToPrettyString(part)} to {ToPrettyString(furnitureComp.DrawerContainer.Owner)}");
        }

        DebugTools.Assert(!furnitureComp.CachedLayout.Values.Contains(GetNetEntity(part)));
        DebugTools.Assert(!furnitureComp.CachedLayout.ContainsKey(offset));

        AddToLayout(furnitureComp, part, offset);
        _appearance.SetData(part.Owner, ModuleFurniturePartVisuals.InFurniture, true);
        DebugTools.Assert(furnitureComp.CachedLayout.Count == furnitureComp.DrawerContainer.Count);
    }

    private void AddToOccupation(ModuleFurnitureComponent furnitureComp, Entity<ModuleFurniturePartComponent> part, Vector2i offset)
    {
        var partSize = part.Comp.ContainerSize;

        for (var height = 0; height < partSize.Y; height++)
        {
            for (var width = 0; width < partSize.X; width++)
            {
                var keyVector = offset + (width, height);
                DebugTools.Assert(!furnitureComp.CachedOccupation[keyVector]);
                furnitureComp.CachedOccupation[keyVector] = true;
            }
        }
    }

    private void FreeOccupation(ModuleFurnitureComponent furnitureComp, Vector2i offset, Entity<ModuleFurniturePartComponent?> part)
    {
        if (!Resolve(part.Owner, ref part.Comp))
            return;

        var partSize = part.Comp.ContainerSize;

        for (var height = 0; height < partSize.Y; height++)
        {
            for (var width = 0; width < partSize.X; width++)
            {
                var keyVector = offset + (width, height);
                DebugTools.Assert(furnitureComp.CachedOccupation[keyVector]);
                furnitureComp.CachedOccupation[keyVector] = false;
            }
        }
    }

    private void AddToLayout(ModuleFurnitureComponent furnitureComp, Entity<ModuleFurniturePartComponent> part, Vector2i offset)
    {
        // we need to offset entity only when it in container
        DebugTools.Assert(furnitureComp.DrawerContainer.Contains(part));

        var spriteOffsetPixels = furnitureComp.StartingDrawerPixelOffset + offset * furnitureComp.PixelPerLayoutTile;
        var intervalOffsetPixel = offset * furnitureComp.DrawerPixelInterval;
        var offsetMeters = (spriteOffsetPixels + intervalOffsetPixel) * Vector2i.DownRight / furnitureComp.PixelPerMeter;
        var roundedOffsetMeters = new Vector2(MathF.Round(offsetMeters.X, 3), MathF.Round(offsetMeters.Y, 3));

        _transform.SetLocalPosition(part, roundedOffsetMeters);
        furnitureComp.CachedLayout.Add(offset, GetNetEntity(part));
    }
}
