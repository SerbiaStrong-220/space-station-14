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
using Robust.Client.Player;
using Content.Shared.SS220.Experience;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Experience;

public sealed class ExperienceViewerUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<ExperienceInfoSystem>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly ExperienceInfoSystem _experienceInfo = default!;

    private ExperienceViewWindow? _window;
    private MenuButton? ExperienceViewButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.ExperienceViewButton;

    public static readonly string FreePointAvailableAnimationKey = "freePointAvailable";

    private static readonly Color BaseColor = Color.White;
    private static readonly Color AnimationColor = Color.White.WithAlpha(0.3f);

    private static readonly float BlinkHalfTime = 0.15f;
    private static readonly float FullAnimationTime = 20f;

    private readonly Animation _freePointAvailableAnimation = new()
    {
        Length = TimeSpan.FromSeconds(FullAnimationTime),
        AnimationTracks =
        {
            new AnimationTrackControlProperty
            {
                Property = nameof(Control.Modulate),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(AnimationColor, BlinkHalfTime),
                    new AnimationTrackProperty.KeyFrame(BaseColor, BlinkHalfTime),
                    new AnimationTrackProperty.KeyFrame(AnimationColor, BlinkHalfTime),
                    new AnimationTrackProperty.KeyFrame(BaseColor, BlinkHalfTime),
                    new AnimationTrackProperty.KeyFrame(AnimationColor, BlinkHalfTime),
                    new AnimationTrackProperty.KeyFrame(BaseColor, BlinkHalfTime),
                    new AnimationTrackProperty.KeyFrame(AnimationColor, BlinkHalfTime),
                    new AnimationTrackProperty.KeyFrame(BaseColor, 0f),
                    new AnimationTrackProperty.KeyFrame(BaseColor, FullAnimationTime)
                }
            }
        }
    };

    private int _freePoints;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_freePoints < 1)
            return;

        var haveAnimation = ExperienceViewButton?.HasRunningAnimation(FreePointAvailableAnimationKey);

        if (haveAnimation is null)
            return;

        if (!haveAnimation.Value)
            ExperienceViewButton?.PlayAnimation(_freePointAvailableAnimation, FreePointAvailableAnimationKey);
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);

        _window = UIManager.CreateWindow<ExperienceViewWindow>();

        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        _window.OnSubmitChangeAction += _experienceInfo.SendChangeEntityExperiencePlayerRequest;

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
        _player.LocalPlayerDetached += CharacterDetached;
        _player.LocalPlayerAttached += CharacterAttached;
    }

    public void OnSystemUnloaded(ExperienceInfoSystem system)
    {
        system.OnExperienceUpdated -= ExperienceUpdated;
        _player.LocalPlayerDetached -= CharacterDetached;
        _player.LocalPlayerAttached -= CharacterAttached;
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

    private void ExperienceUpdated(ExperienceData data, int freePoints)
    {
        _freePoints = freePoints;

        if (_freePoints < 1)
            ExperienceViewButton?.StopAnimation(FreePointAvailableAnimationKey);

        if (_window == null)
            return;

        _window.SetSkillDictionary(data.SkillDictionary);
        _window.SetKnowledge(data.Knowledges);
        _window.SetFreeSublevelPoints(freePoints);
    }

    private void CharacterDetached(EntityUid _)
    {
        CloseWindow();
    }

    private void CharacterAttached(EntityUid _)
    {
        _experienceInfo.RequestLocalPlayerExperienceData();
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
