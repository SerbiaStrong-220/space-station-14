// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Database.SS220.PendingBans;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Administration.PendingBans;

public sealed class PendingBanControl : BoxContainer
{
    public event Action? ScheduleChanged;

    private readonly CheckBox _enabled;
    private readonly OptionButton _trigger;
    private readonly LineEdit _delay;
    private readonly BoxContainer _options;
    private readonly BoxContainer _delayRow;

    public bool IsPending => _enabled.Pressed;

    public PendingBanControl()
    {
        Orientation = LayoutOrientation.Vertical;
        _enabled = new CheckBox { Text = Loc.GetString("pending-ban-enable") };
        AddChild(_enabled);
        _options = new BoxContainer { Orientation = LayoutOrientation.Vertical, Visible = false };
        AddChild(_options);
        _trigger = new OptionButton();
        _trigger.AddItem(Loc.GetString("pending-ban-delay"), (int)PendingBanTrigger.Delay);
        _trigger.AddItem(Loc.GetString("pending-ban-round-end"), (int)PendingBanTrigger.RoundEnd);
        _trigger.AddItem(Loc.GetString("pending-ban-round-start"), (int)PendingBanTrigger.RoundStart);
        _trigger.AddItem(Loc.GetString("pending-ban-lobby"), (int)PendingBanTrigger.Lobby);
        _trigger.SelectId((int)PendingBanTrigger.Delay);
        _options.AddChild(_trigger);
        _delayRow = new BoxContainer();
        _delay = new LineEdit
        {
            Text = "5",
            MinWidth = 80,
            ToolTip = Loc.GetString("pending-ban-delay-help"),
        };
        _delayRow.AddChild(_delay);
        _delayRow.AddChild(new Label { Text = Loc.GetString("pending-ban-minutes") });
        _options.AddChild(_delayRow);
        _options.AddChild(new Label
        {
            Text = Loc.GetString("pending-ban-duration-hint"),
            ToolTip = Loc.GetString("pending-ban-shutdown-hint"),
        });
        _enabled.OnToggled += _ =>
        {
            _options.Visible = IsPending;
            ScheduleChanged?.Invoke();
        };
        _trigger.OnItemSelected += args =>
        {
            _trigger.SelectId(args.Id);
            _delayRow.Visible = args.Id == (int)PendingBanTrigger.Delay;
            ScheduleChanged?.Invoke();
        };
        _delay.OnTextChanged += _ =>
        {
            _delay.ModulateSelfOverride = null;
            ScheduleChanged?.Invoke();
        };
    }

    public bool TryGetSchedule(out PendingBanTrigger trigger, out uint minutes)
    {
        trigger = IsPending ? (PendingBanTrigger)_trigger.SelectedId : PendingBanTrigger.Immediate;
        minutes = 0;
        if (trigger != PendingBanTrigger.Delay)
            return true;

        if (uint.TryParse(_delay.Text, out minutes) && minutes is > 0 and <= 525600)
            return true;

        _delay.ModulateSelfOverride = Color.Red;
        _delay.GrabKeyboardFocus();
        return false;
    }
}
