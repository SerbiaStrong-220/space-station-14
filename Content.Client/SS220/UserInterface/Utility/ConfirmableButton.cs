// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Threading.Tasks;

namespace Content.Client.SS220.UserInterface.Utility;

/// <summary>
///     A button that requires some confirmation clicks before executing its action.
/// </summary>
[Virtual]
public class ConfirmableButton : Button
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public event Action? OnConfirmed;
    public event Action? OnTimeout;

    [ViewVariables]
    public ConfirmableButtonClicksCounterAction CounterActionOnConfirmed = ConfirmableButtonClicksCounterAction.Reset;
    [ViewVariables]
    public ConfirmableButtonClicksCounterAction CounterActionOnTimeout = ConfirmableButtonClicksCounterAction.Reset;

    [ViewVariables]
    public float TimeoutDelayMilliseconds { get; set; } = 3000f;
    public TimeSpan TimeoutDelay => TimeSpan.FromMilliseconds(TimeoutDelayMilliseconds);

    [ViewVariables]
    public uint ClicksForConfirm
    {
        get => _clicksForConfirm;
        set => _clicksForConfirm = Math.Max(1, value);
    }
    private uint _clicksForConfirm = 2;

    [ViewVariables]
    public string? DefaultText
    {
        get => _defaultState.Text;
        set
        {
            var state = _defaultState;
            state.Text = value;
            SetClickState(0, state);
        }
    }

    [ViewVariables]
    public Color? DefaultColor
    {
        get => _defaultState.Color;
        set
        {
            var state = _defaultState;
            state.Color = value;
            SetClickState(0, state);
        }
    }

    private ConfirmableButtonState _defaultState = new();

    private static readonly TimeSpan LoopedUpdateDelay = TimeSpan.FromMilliseconds(100);

    private TimeSpan _timeout = TimeSpan.Zero;

    private uint _clicks = 0;
    private readonly Dictionary<uint, ConfirmableButtonState> _clickStates = [];

    public ConfirmableButton()
    {
        IoCManager.InjectDependencies(this);

        OnPressed += _ => IncreaseCounter();

        SetClickState(0, _defaultState);
        LoopedUpdate();
    }

    public ConfirmableButton(ConfirmableButtonState defaultState) : this()
    {
        SetClickState(0, defaultState);
    }

    public ConfirmableButton(string? text, Color? overrideColor) : this(new ConfirmableButtonState(text, overrideColor)) { }

    public void SetClickState(Dictionary<uint, ConfirmableButtonState> clickStates)
    {
        foreach (var (key, value) in clickStates)
            SetClickState(key, value);
    }

    public void SetClickState(uint click, ConfirmableButtonState state)
    {
        if (click == 0)
            _defaultState = state;

        _clickStates[click] = state;
        UpdateState();
    }

    public void Update()
    {
        if (Disposed)
            return;

        DebugTools.Assert(ClicksForConfirm != 0);
        if (_clicks >= ClicksForConfirm)
            Confirmed();

        if (_clicks != 0 && _gameTiming.CurTime >= _timeout)
            Timeout();

        UpdateState();
    }

    private async void LoopedUpdate()
    {
        await Task.Delay(LoopedUpdateDelay);
        LoopedUpdate();

        if (_clicks != 0)
            Update();
    }

    private void UpdateState()
    {
        if (Disposed)
            return;

        if (_clickStates.TryGetValue(_clicks, out var state))
        {
            Text = state.Text;
            ModulateSelfOverride = state.Color;
        }
    }

    private void Confirmed()
    {
        OnConfirmed?.Invoke();
        ProcessCounterAction(CounterActionOnConfirmed);
    }

    private void Timeout()
    {
        OnTimeout?.Invoke();
        ProcessCounterAction(CounterActionOnTimeout);
    }

    public void SetClicksCounter(uint value)
    {
        _timeout = _gameTiming.CurTime + TimeoutDelay;
        _clicks = value;
        Update();
    }

    public void ResetCounter()
    {
        SetClicksCounter(0);
    }

    public void IncreaseCounter()
    {
        SetClicksCounter(_clicks + 1);
    }

    public void DecreaseCounter()
    {
        SetClicksCounter(_clicks - 1);
    }

    private void ProcessCounterAction(ConfirmableButtonClicksCounterAction action)
    {
        switch (action)
        {
            case ConfirmableButtonClicksCounterAction.Decrease:
                DecreaseCounter();
                break;

            case ConfirmableButtonClicksCounterAction.Increase:
                IncreaseCounter();
                break;

            case ConfirmableButtonClicksCounterAction.Reset:
                ResetCounter();
                break;
        }
    }
}

public record struct ConfirmableButtonState(string? Text, Color? Color);

public enum ConfirmableButtonClicksCounterAction
{
    None,
    Decrease,
    Increase,
    Reset
}
