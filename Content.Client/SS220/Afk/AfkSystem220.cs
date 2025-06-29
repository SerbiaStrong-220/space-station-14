// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Afk;
using Content.Shared.SS220.CCVars;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Afk;

public sealed class AfkSystem220 : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private bool _isAnyInput = false;
    private TimeSpan? _lastActivityMessageTimestamp;
    private TimeSpan _activityMessageInterval;

    public override void Initialize()
    {
        base.Initialize();
        _inputManager.UIKeyBindStateChanged += OnUIKeyBindStateChanged;
        _activityMessageInterval = TimeSpan.FromSeconds(_configurationManager.GetCVar(CCVars220.AfkActivityMessageInterval));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        // The problem here is that shutdown can be very likely caused by disconnecting from server,
        // that is caused by clicking the UI button, that is caused by user input and this UIKeyBindStateChanged event,
        // so this is basically a modifying-collection-inside-iteration case. That is why I use DeferAction, so
        // unsubscription will happen this or next frame but certainly not inside collection iteration.
        // Yes, this is not the best solution, but probably the simplest for now.
        _userInterfaceManager.DeferAction(() =>
        {
            _inputManager.UIKeyBindStateChanged -= OnUIKeyBindStateChanged;
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // We need to initialize time and turns out that it does not work inside Initialize() call.
        _lastActivityMessageTimestamp ??= _gameTiming.CurTime;

        if (_gameTiming.CurTime - _lastActivityMessageTimestamp > _activityMessageInterval)
        {
            SendActivityMessage();
        }
    }

    private bool OnUIKeyBindStateChanged(BoundKeyEventArgs args)
    {
        _isAnyInput = true;
        return false;
    }

    private void SendActivityMessage()
    {
        _lastActivityMessageTimestamp = _gameTiming.CurTime;
        if (!_isAnyInput)
            return;
        RaiseNetworkEvent(new PlayerActivityMessage());
        _isAnyInput = false;
    }
}
