// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Gameplay;
using Content.Client.SS220.Experience.Ui;
using Content.Client.UserInterface.Controls;
using Content.Shared.SS220.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Experience;

public sealed class ExperienceViewerUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<ExperienceInfoSystem>
{
    [UISystemDependency] private readonly ExperienceInfoSystem _experienceInfo = default!;

    private ExperienceViewWindow? _window;
    private MenuButton? ExperienceViewButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.ExperienceViewButton;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<ExperienceViewWindow>();

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        CommandBinds.Builder
            .Bind(KeyFunctions220.OpenExperienceViewerMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<ExperienceViewerUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        _window?.Close();
        _window = null;

        CommandBinds.Unregister<ExperienceViewerUIController>();
    }

    public void OnSystemLoaded(ExperienceInfoSystem system)
    {
        system.OnExperienceUpdated += ExperienceUpdated;
        // _player.LocalPlayerDetached += CharacterDetached;
    }

    public void OnSystemUnloaded(ExperienceInfoSystem system)
    {
        system.OnExperienceUpdated -= ExperienceUpdated;
        // _player.LocalPlayerDetached -= CharacterDetached;
    }

    public void UnloadButton()
    {
        if (ExperienceViewButton == null)
            return;

        ExperienceViewButton.OnPressed -= ExperienceViewButtonPressed;
    }

    public void LoadButton()
    {
        if (ExperienceViewButton == null)
            return;

        ExperienceViewButton.OnPressed += ExperienceViewButtonPressed;
    }

    private void ExperienceUpdated(ExperienceData data)
    {
        if (_window == null)
            return;

        _window.SetSkillDictionary(data.SkillDictionary);
    }

    private void DeactivateButton()
    {
        if (ExperienceViewButton == null)
            return;

        ExperienceViewButton.Pressed = false;
    }

    private void ActivateButton()
    {
        if (ExperienceViewButton == null)
            return;

        ExperienceViewButton.Pressed = true;
    }

    private void ExperienceViewButtonPressed(Button.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        ExperienceViewButton?.SetClickPressed(!_window.IsOpen);

        if (_window.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            _experienceInfo.RequestLocalPlayerExperienceData();
            _window.Open();
        }
    }
}
