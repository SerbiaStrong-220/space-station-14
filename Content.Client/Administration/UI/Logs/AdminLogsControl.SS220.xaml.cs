// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Administration.UI.CustomControls;
using Content.Shared.Administration.Logs;
using Robust.Client.Animations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Animations;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Text;

namespace Content.Client.Administration.UI.Logs;

public sealed partial class AdminLogsControl : Control
{
    private readonly TimeSpan _buttonSafetyFreeDuration = TimeSpan.FromSeconds(3f);
    private TimeSpan? _buttonSafetyFreeEnd = null;

    private readonly ResPath _selectorIndicatorTexture = new("/Textures/Interface/VerbIcons/plus.svg.192dpi.png");

    private readonly Color _hoverSelectOffColor = Color.FromHex("#d5d5d5ff");
    private readonly Color _hoverSelectOnColor = Color.FromHex("#b3f4afff");

    /// <summary>
    /// Defines if we enter mode where every mouse hover selects (or deselects) log entry
    /// </summary>
    private bool _hoverSelectMode = false;

    public bool HoverSelectMode
    {
        get => _hoverSelectMode;
        set
        {
            SetHoverSelectMode(value);
            SetHoverSelectModeView(value);
            _hoverSelectMode = value;
        }
    }
    // admin-logs-time-filter start
    private readonly Color _validDateBorderColor = Color.FromHex("#88f19dff");
    // this is orange because being invalid means just pass check
    private readonly Color _invalidDateBorderColor = Color.FromHex("#fcd175ff");

    public bool EarlyDateValid
    {
        get => _earlyDateValid;
        set
        {
            if (value)
                EarlyBorderTime.ModulateSelfOverride = _validDateBorderColor;
            else
                EarlyBorderTime.ModulateSelfOverride = _invalidDateBorderColor;

            _earlyDateValid = value;
        }
    }

    public bool LateDateValid
    {
        get => _lateDateValid;
        set
        {
            if (value)
                LateBorderTime.ModulateSelfOverride = _validDateBorderColor;
            else
                LateBorderTime.ModulateSelfOverride = _invalidDateBorderColor;

            _lateDateValid = value;
        }
    }

    private DateTime? _earlyDateBorder;
    private bool _earlyDateValid = false;
    private DateTime? _lateDateBorder;
    private bool _lateDateValid = false;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // This checks for null for free, do not invert it as null always produces a false value
        if (_gameTiming.CurTime > _buttonSafetyFreeEnd)
        {
            UnfreeClearSelection();
        }
    }

    private void UnfreeClearSelection()
    {
        _buttonSafetyFreeEnd = null;
        ClearSelection.ModulateSelfOverride = null;
        ClearSelection.Text = Loc.GetString("admin-logs-clear-selection");
    }

    private bool PassEarlyTimeFilter(SharedAdminLog log)
    {
        if (!EarlyDateValid)
            return true;

        if (log.Date < _earlyDateBorder)
            return false;

        return true;
    }
    private bool PassLateTimeFilter(SharedAdminLog log)
    {
        if (!LateDateValid)
            return true;

        if (log.Date > _lateDateBorder)
            return false;

        return true;
    }

    private void OnEarlyDateTimeChanged(LineEdit.LineEditEventArgs args)
    {
        if (!DateTime.TryParse(args.Text, out var result))
        {
            EarlyDateValid = false;
            EarlyBorderTime.Clear();
            UpdateLogs();
            return;
        }

        _earlyDateBorder = result;

        //sometimes shit happens
        if (_earlyDateBorder is null)
            return;

        EarlyBorderTime.Text = _earlyDateBorder.Value.ToString();
        EarlyDateValid = true;

        UpdateLogs();
    }

    private void OnLateDateTimeChanged(LineEdit.LineEditEventArgs args)
    {
        if (!DateTime.TryParse(args.Text, out var result))
        {
            LateDateValid = false;
            LateBorderTime.Clear();
            UpdateLogs();
            return;
        }

        _lateDateBorder = result;

        //sometimes shit happens
        if (_lateDateBorder is null)
            return;

        LateBorderTime.Text = _lateDateBorder.Value.ToString();
        LateDateValid = true;

        UpdateLogs();
    }
    // admin-logs-time-filter end

    // add-logs-copying begin
    private void OnCopyToClipboard()
    {
        var builder = new StringBuilder();
        int totalLogCopied = 0;
        foreach (var child in LogsContainer.Children.Reverse())
        {
            if (child is not AdminLogLabel log)
                continue;

            if (!log.MarkedForCopying)
                continue;

            builder.AppendLine(log.Text);
            totalLogCopied++;
        }

        _clipboard.SetText(builder.ToString());
        if (!CopyToClipboard.HasRunningAnimation("work"))
            CopyToClipboard.PlayAnimation(_copyingEnded, "work");
        _popup.PopupCursor(Loc.GetString("admin-logs-copy-clipboard-popup", ("count", totalLogCopied)), Shared.Popups.PopupType.Medium);
    }

    private readonly Animation _copyingEnded = new()
    {
        Length = TimeSpan.FromSeconds(0.5),
        AnimationTracks =
        {
            new AnimationTrackControlProperty
            {
                Property = nameof(Control.Modulate),
                InterpolationMode = AnimationInterpolationMode.Linear,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Color.White, 0f),
                    new AnimationTrackProperty.KeyFrame(Color.LimeGreen, 0.1f),
                    new AnimationTrackProperty.KeyFrame(Color.SeaGreen, 0.2f),
                    new AnimationTrackProperty.KeyFrame(Color.LimeGreen, 0.1f),
                    new AnimationTrackProperty.KeyFrame(Color.White, 0.1f)
                }
            }
        }
    };

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Handled || args.Function != EngineKeyFunctions.UIRightClick)
            return;

        HoverSelectMode = true;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Handled || args.Function != EngineKeyFunctions.UIRightClick)
            return;

        HoverSelectMode = false;
    }

    private void SetHoverSelectModeView(bool hoverSelectMode)
    {
        if (hoverSelectMode)
            SelectorModeIndicator.DisplayRect.ModulateSelfOverride = _hoverSelectOnColor;
        else
            SelectorModeIndicator.DisplayRect.ModulateSelfOverride = _hoverSelectOffColor;
    }

    private void SetHoverSelectMode(bool hoverSelectMode)
    {
        foreach (var child in LogsContainer.Children)
        {
            if (child is not AdminLogLabel log)
                continue;

            log.SelectHoverMode = hoverSelectMode;
        }
    }
}
