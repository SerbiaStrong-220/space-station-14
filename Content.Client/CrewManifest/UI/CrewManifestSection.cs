using Content.Shared.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Roles;
using Content.Client.SS220.UserInterface;
using Content.Shared.PDA;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestSection : BoxContainer
{
    public CrewManifestSection(
        IEntityManager entMan, // ss220 add additional info for pda
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        DepartmentPrototype section,
        List<CrewManifestEntry> entries,
        Entity<PdaComponent>? pda = null) // ss220 add additional info for pda
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = Loc.GetString(section.Name)
        });

        var gridContainer = new GridContainer()
        {
            HorizontalExpand = true,
            //Columns = 2, // ss220 add additional info for pda start
        };

        AddChild(gridContainer);

        foreach (var entry in entries)
        {
            var name = new CopyableRichTextLabel() // SS220-QoL copy name from manifest button
            {
                HorizontalExpand = true,
            };
            name.SetMessage(entry.Name);

            var titleContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            var title = new CopyableRichTextLabel(); // SS220-QoL copy name from manifest button
            title.SetMessage(entry.JobTitle);

            if (prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
            {
                var icon = new TextureRect()
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    Texture = spriteSystem.Frame0(jobIcon.Icon),
                    Margin = new Thickness(0, 0, 4, 0)
                };

                titleContainer.AddChild(icon);
                titleContainer.AddChild(title);
            }
            else
            {
                titleContainer.AddChild(title);
            }

            // ss220 add additional info for pda start
            if (pda is { Comp.ContainedId: not null })
            {
                var containerButton = new BoxContainer
                {
                    HorizontalExpand = true,
                };

                var linkButton = new Button
                {
                    Text = Loc.GetString("records-manifest-link-button-text"),
                    SetSize = new Vector2(32, 32),
                    ToolTip = Loc.GetString("records-manifest-link-button-tooltip"),
                    VerticalAlignment = VAlignment.Center,
                };

                containerButton.AddChild(name);
                containerButton.AddChild(titleContainer);
                containerButton.AddChild(linkButton);

                linkButton.OnPressed += _ =>
                {
                    var netPda = entMan.GetNetEntity(pda.Value);
                    entMan.RaisePredictiveEvent(new RequestLinkIdToRecord(netPda, entry.Key));
                };

                gridContainer.AddChild(containerButton);
            }
            else
            {
                gridContainer.Columns = 2;
                gridContainer.AddChild(name);
                gridContainer.AddChild(titleContainer);
            }
            // ss220 add additional info for pda end
        }
    }
}
