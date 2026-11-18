// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.SS220.Surgery.SurgeryStartUi;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Alert;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Alerts.Widgets;

public sealed partial class AlertsUI
{
    private const float PathologyPuppetScale = 2f;
    private static readonly ProtoId<AlertCategoryPrototype> HealthAlertCategory = "Health";

    private SurgeryPuppetBox? _pathologyPuppet;

    public void SetPathologyTarget(EntityUid? target, IEntityManager entMan)
    {
        if (target == null ||
            !entMan.TryGetComponent<PathologyHolderComponent>(target.Value, out var holder) ||
            holder.LifeStage >= ComponentLifeStage.Stopping)
        {
            ClearPathologyPuppet();
            return;
        }

        if (_pathologyPuppet == null)
        {
            _pathologyPuppet = new SurgeryPuppetBox
            {
                Interactive = false,
                ShowBackground = false,
                ShowTooltips = false,
                Scale = new Vector2(PathologyPuppetScale, PathologyPuppetScale),
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0),
            };
            _pathologyPuppet.Initialize();
        }

        _pathologyPuppet.Visible = true;
        _pathologyPuppet.UpdateFromTarget(target);
        UpdatePathologyPuppetPosition();
    }

    private void ClearPathologyPuppet()
    {
        if (_pathologyPuppet == null)
            return;

        _pathologyPuppet.Visible = false;
        _pathologyPuppet.UpdateFromTarget(null);
        _pathologyPuppet.Parent?.RemoveChild(_pathologyPuppet);
    }

    private void UpdatePathologyPuppetPosition()
    {
        if (_pathologyPuppet is not { Visible: true })
            return;

        var desiredIndex = 0;
        var currentIndex = -1;
        var seenBefore = 0;

        for (var i = 0; i < AlertContainer.ChildCount; i++)
        {
            var child = AlertContainer.GetChild(i);
            if (child == _pathologyPuppet)
            {
                currentIndex = i;
                continue;
            }

            if (child is AlertControl control && control.Alert.Category == HealthAlertCategory)
                desiredIndex = seenBefore + 1;

            seenBefore++;
        }

        if (currentIndex == desiredIndex)
            return;

        if (_pathologyPuppet.Parent != AlertContainer)
            AlertContainer.AddChild(_pathologyPuppet);

        _pathologyPuppet.SetPositionInParent(desiredIndex);
    }
}
