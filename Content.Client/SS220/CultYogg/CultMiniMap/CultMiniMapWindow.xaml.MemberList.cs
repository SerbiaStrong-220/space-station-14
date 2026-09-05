// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Mobs;
using Content.Shared.SS220.CultYogg.CultMiniMap;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.CultYogg.CultMiniMap;

public sealed partial class CultMiniMapWindow
{
    private void RefreshMemberList()
    {
        MembersTable.RemoveAllChildren();
        _buttons.Clear();

        var visibleMembers = _trackedEntities
            .Where(entity => entity.Marker.ShowInList && MatchesSearch(entity))
            .ToList();
        var self = visibleMembers.FirstOrDefault(member => member.Entity == _owner);
        if (self != null)
            AddMemberSection(self.Marker, new[] { self });

        foreach (var group in visibleMembers
                     .Where(member => member.Entity != _owner)
                     .GroupBy(member => member.Marker.RuleIndex))
        {
            AddMemberSection(group.First().Marker, group.OrderBy(member => member.Name));
        }

        EmptyLabel.Visible = _buttons.Count == 0;
        UpdateSelection();
    }

    private bool MatchesSearch(CultMiniMapTrackedEntity member)
    {
        var role = GetMarkerLabel(member.Marker);
        return member.Name.Contains(SearchLineEdit.Text, StringComparison.CurrentCultureIgnoreCase)
               || role.Contains(SearchLineEdit.Text, StringComparison.CurrentCultureIgnoreCase);
    }

    private void AddMemberSection(CultMiniMapMarker marker, IEnumerable<CultMiniMapTrackedEntity> members)
    {
        var entries = members.ToList();
        if (entries.Count == 0)
            return;

        if (MembersTable.ChildCount > 0)
            MembersTable.AddChild(new Control { SetHeight = 12 });

        var header = new BoxContainer
        {
            HorizontalExpand = true,
            Margin = new Thickness(10, 0, 4, 3),
        };
        header.AddChild(CreateMarkerIcon(marker, 20f));
        header.AddChild(new Label
        {
            Text = Loc.GetString("cult-mini-map-group", ("group", GetMarkerLabel(marker)), ("count", entries.Count)),
            StyleClasses = { "LabelHeading" },
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        });
        MembersTable.AddChild(header);

        foreach (var member in entries)
            AddMemberRow(member);
    }

    private void AddMemberRow(CultMiniMapTrackedEntity member)
    {
        var coordinates = _entities.GetCoordinates(member.Coordinates);
        var tooltip = member.Name;
        if (member.Marker.ShowHealth)
            tooltip += "\n" + GetHealthText(member);
        if (coordinates == null)
            tooltip += "\n" + Loc.GetString("cult-mini-map-unavailable");

        var button = new Button
        {
            HorizontalExpand = true,
            Disabled = coordinates == null || !NavMap.Visible,
            ToolTip = tooltip,
        };

        var row = new BoxContainer { HorizontalExpand = true };
        row.AddChild(CreateMarkerIcon(member.Marker, 16f));

        if (member.Marker.ShowHealth)
            row.AddChild(CreateHealthIcon(member));

        row.AddChild(new Label
        {
            Text = member.Name,
            HorizontalExpand = true,
            ClipText = true,
        });
        string? status = null;
        if (coordinates == null)
            status = Loc.GetString("cult-mini-map-unavailable");
        else if (member.Marker.ShowHealth)
            status = GetHealthStatus(member);

        if (status != null)
        {
            row.AddChild(new Label
            {
                Text = status,
                Margin = new Thickness(6, 0, 0, 0),
            });
        }

        button.AddChild(row);
        button.OnPressed += _ => SelectMember(member.Entity == _selected ? null : member.Entity);
        _buttons.Add(member.Entity, button);
        MembersTable.AddChild(button);
    }

    private static AnimatedTextureRect CreateHealthIcon(CultMiniMapTrackedEntity member)
    {
        var healthIcon = new AnimatedTextureRect
        {
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
            ToolTip = GetHealthText(member),
            Modulate = member.HealthState == MobState.Invalid ? Color.Gray : Color.White,
        };
        healthIcon.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(
            new ResPath("Interface/Alerts/human_crew_monitoring.rsi"), GetHealthIconState(member)));
        healthIcon.DisplayRect.TextureScale = new Vector2(2f);
        return healthIcon;
    }

    private TextureRect CreateMarkerIcon(CultMiniMapMarker marker, float size)
    {
        return new TextureRect
        {
            Texture = _sprites.Frame0(marker.Icon),
            SetSize = new Vector2(size),
            CanShrink = true,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Modulate = marker.Color,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
        };
    }

    private static string GetMarkerLabel(CultMiniMapMarker marker)
    {
        return marker.Label is { } label ? Loc.GetString(label) : marker.Component;
    }

    private static string GetHealthIconState(CultMiniMapTrackedEntity member)
    {
        // Use the actual mob state: critical/dead mobs must never look healthy,
        // even if their state was changed independently of damage thresholds.
        if (member.HealthState == MobState.Dead)
            return "dead";
        if (member.HealthState == MobState.Critical)
            return "critical";
        if (member.HealthState != MobState.Alive || member.DamagePercentage is not { } damage)
            return "alive";

        var index = (int) MathF.Round(4f * Math.Clamp(damage, 0f, 1f));
        return "health" + index;
    }

    private static string GetHealthStatus(CultMiniMapTrackedEntity member)
    {
        return Loc.GetString(member.HealthState switch
        {
            MobState.Alive => "cult-mini-map-health-alive",
            MobState.Critical => "cult-mini-map-health-critical",
            MobState.Dead => "cult-mini-map-health-dead",
            _ => "cult-mini-map-health-unknown",
        });
    }

    private static string GetHealthText(CultMiniMapTrackedEntity member)
    {
        var status = GetHealthStatus(member);
        if (member.HealthState == MobState.Invalid)
            return status;

        var damage = member.DamagePercentage is { } percentage
            ? Loc.GetString("cult-mini-map-health-damage", ("percent", MathF.Round(percentage * 100f)))
            : Loc.GetString("cult-mini-map-health-no-damage");
        return status + "\n" + damage;
    }
}
