// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.UserInterface;
using Content.Shared.SS220.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client.SS220.CrewManifest.UI;

/// <summary>
///     Draws the synthetics section of the crew manifest.
/// </summary>
public sealed partial class CrewManifestSiliconSection : BoxContainer
{
    private const string SiliconDepartmentLocId = "department-Silicon";

    public CrewManifestSiliconSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        SyntheticManifestEntry[] entries)
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = Loc.GetString(SiliconDepartmentLocId)
        });

        var gridContainer = new GridContainer()
        {
            HorizontalExpand = true,
            Columns = 2
        };

        AddChild(gridContainer);

        foreach (var entry in entries)
        {
            var name = new CopyableRichTextLabel()
            {
                HorizontalExpand = true,
            };
            name.SetMessage(entry.Name);

            var infoContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            if (entry.JobIcon != null &&
                prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
            {
                infoContainer.AddChild(CreateIcon(spriteSystem, jobIcon.Icon));
            }

            var chassis = new Label()
            {
                Text = entry.IsAi
                    ? entry.ChassisName
                    : Loc.GetString("silicon-manifest-chassis", ("chassis", entry.ChassisName)),
                Modulate = Color.Gray,
                VerticalAlignment = VAlignment.Center
            };

            infoContainer.AddChild(chassis);

            gridContainer.AddChild(name);
            gridContainer.AddChild(infoContainer);
        }
    }

    private static TextureRect CreateIcon(SpriteSystem spriteSystem, SpriteSpecifier sprite)
    {
        return new TextureRect()
        {
            TextureScale = new Vector2(2, 2),
            VerticalAlignment = VAlignment.Center,
            Texture = spriteSystem.Frame0(sprite),
            Margin = new Thickness(0, 0, 4, 0)
        };
    }
}

