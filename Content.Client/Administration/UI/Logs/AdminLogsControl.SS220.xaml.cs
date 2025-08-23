using Content.Client.Administration.UI.CustomControls;
using Content.Shared.Administration.Logs;
using Robust.Client.Animations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Animations;
using System.Linq;
using System.Text;

namespace Content.Client.Administration.UI.Logs;

public sealed partial class AdminLogsControl : Control
{
    // admin-logs-time-filter start
    private readonly Color _validDateBorderColor = Color.FromHex("#88f19dff");
    // this is orange because being invalid means just pass check
    private readonly Color _invalidDateBorderColor = Color.FromHex("#fcd175ff");

    public bool EarlyDateValid
    {
        get { return _earlyDateValid; }
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
        get { return _lateDateValid; }
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

        foreach (var child in LogsContainer.Children.Reverse())
        {
            if (child is not AdminLogLabel log)
            {
                continue;
            }

            if (!log.MarkedForCopying)
                continue;

            builder.AppendLine(log.Text);
        }

        _clipboard.SetText(builder.ToString());
        if (!CopyToClipboard.HasRunningAnimation("work"))
            CopyToClipboard.PlayAnimation(_copyingEnded, "work");
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

}
