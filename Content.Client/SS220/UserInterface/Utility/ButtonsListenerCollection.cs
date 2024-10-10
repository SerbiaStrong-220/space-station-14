// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.UserInterface.Utility;

/// <summary>
/// This class is a bit weird piece of code, but the main point of it
/// is that you can not notify listeners of the button press via code.
/// </summary>
/// <remarks>
/// Do not forget to call <see cref="Update(TimeSpan)"/> if you want your pressed buttons to un-press.
/// </remarks>
public sealed class ButtonsListenerCollection
{
    public IEnumerable<BaseButton> Buttons => _buttons.Keys;

    private readonly Dictionary<BaseButton, List<Action<BaseButton.ButtonEventArgs?>>> _buttons = new();

    private BaseButton? _fakePressedButton;
    private TimeSpan _stopPressingTimer;

    public void Update(TimeSpan deltaSpan)
    {
        if (_fakePressedButton is null)
        {
            return;
        }
        _stopPressingTimer -= deltaSpan;
        if (_stopPressingTimer > TimeSpan.Zero)
        {
            return;
        }
        _fakePressedButton.SetClickPressed(false);
        _fakePressedButton = null;
    }

    public void ListenButton(BaseButton button, Action<BaseButton.ButtonEventArgs?> callback)
    {
        if (_buttons.TryGetValue(button, out var callbacks))
        {
            callbacks.Add(callback);
        }
        else
        {
            _buttons.Add(button, new() { callback });
            button.OnPressed += OnButtonPressed;
        }
    }

    public void ListenAllButtons(Action<BaseButton.ButtonEventArgs?> callback)
    {
        foreach (var (_, callbacks) in _buttons)
        {
            callbacks.Add(callback);
        }
    }

    public bool RemoveButtonListener(BaseButton button, Action<BaseButton.ButtonEventArgs?> callback)
    {
        if (!_buttons.TryGetValue(button, out var callbacks))
        {
            return false;
        }
        return callbacks.Remove(callback);
    }

    public bool RemoveButton(BaseButton button)
    {
        if (!_buttons.Remove(button))
        {
            return false;
        }
        button.OnPressed -= OnButtonPressed;
        return true;
    }

    public void Clear()
    {
        foreach (var (button, _) in _buttons)
        {
            button.OnPressed -= OnButtonPressed;
        }
        _buttons.Clear();
    }

    public void PressButton(BaseButton button, TimeSpan pressedDuration, BaseButton.ButtonEventArgs? args = null)
    {
        button.SetClickPressed(true);
        if (_fakePressedButton is { })
        {
            _fakePressedButton.SetClickPressed(false);
        }
        _stopPressingTimer = pressedDuration;
        _fakePressedButton = button;
        NotifyButtonListeners(button, args);
    }

    public void NotifyButtonListeners(BaseButton button, BaseButton.ButtonEventArgs? args)
    {
        if (!_buttons.TryGetValue(button, out var callbacks))
        {
            return;
        }
        foreach (var callback in callbacks)
        {
            callback(args);
        }
    }

    private void OnButtonPressed(BaseButton.ButtonEventArgs? args)
    {
        if (args is null)
        {
            return;
        }
        NotifyButtonListeners(args.Button, args);
    }
}
