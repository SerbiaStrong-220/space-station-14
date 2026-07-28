using System.Linq; // SS220-emote-wheel-rework
using Content.Client.Gameplay;
using Content.Client.Lobby; // SS220-emote-wheel-rework
using Content.Client.SS220.EmoteWheel; // SS220-emote-wheel-rework
using Content.Shared.Preferences; // SS220-emote-wheel-rework
using Robust.Client.UserInterface.CustomControls; // SS220-emote-wheel-rework
using Robust.Shared.Configuration; // SS220-emote-wheel-rework
using System.Numerics; // SS220-emote-wheel-rework
using Content.Client.UserInterface.Controls;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid; // SS220-emote-wheel-rework
using Content.Shared.Input;
using Content.Shared.Speech;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing; // SS220-emote-wheel-rework
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Emotes;

[UsedImplicitly]
public sealed class EmotesUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // SS220-emote-wheel-rework
    [Dependency] private readonly IConfigurationManager _configuration = default!; // SS220-emote-wheel-rework
    [Dependency] private readonly IClientPreferencesManager _preferences = default!; // SS220-emote-wheel-rework

    private MenuButton? EmotesButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.EmotesButton;
    private SimpleRadialMenu? _menu;

    // SS220-emote-wheel-rework-begin

    /// <summary>
    /// How long the menu key may be held before releasing it counts as a flick selection rather than as
    /// a tap that latches the wheel open for clicking.
    /// </summary>
    private static readonly TimeSpan TapLatchThreshold = TimeSpan.FromSeconds(0.2);

    private static readonly SimpleRadialMenuSettings MenuSettings = new()
    {
        CenterFirstItemAtTop = true,

        // Fixed geometry rather than the default "radius grows with button count": the ring should not
        // move under the player's muscle memory as the emote list changes. The inner radius is where the
        // sector starts, so it doubles as the hub size and removes the dead gap the old wheel had.
        // Sized so a full eight sectors each have room for a wrapped two-line name. The outer radius is
        // capped at 300 on purpose: the window is twice that plus the indicator bands, which still fits a
        // 1366x768 screen with room to spare.
        FixedRadius = 218f,
        FixedInnerRadius = 104f,
        FixedOuterRadius = 300f,

        ShowLabels = true,
        // The sector chord at eight buttons on a 218 radius is about 167px, and labels sit below the icon
        // where the chord is wider still, so 156 stays clear of its neighbours. The wrap budget is short
        // deliberately: breaking a long name onto two lines costs nothing, whereas guessing too long
        // clips it, and Cyrillic names run wider per character than the Latin ones.
        LabelMaxWidth = 156f,
        LabelWrapChars = 14,
        ShowHubLabel = true,
        HighlightExpansion = 8f,
    };

    private TimeSpan _menuOpenedAt;
    private DefaultWindow? _editorWindow;

    // SS220-emote-wheel-rework-end

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenEmotesMenu,
                // SS220-emote-wheel-rework: hold to aim, release to emote
                InputCmdHandler.FromDelegate(_ => OnMenuKeyDown(), _ => OnMenuKeyUp()))
            .Register<EmotesUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<EmotesUIController>();
    }

    // SS220-emote-wheel-rework-begin

    private void OnMenuKeyDown()
    {
        // A second press while the wheel is latched open from an earlier tap dismisses it again.
        if (_menu != null)
        {
            CloseMenu();
            return;
        }

        OpenMenu(pointerDriven: true);
    }

    private void OnMenuKeyUp()
    {
        if (_menu is not { PointerMode: true })
            return;

        // A quick tap that never left the hub means the player wants the wheel to stay up and be clicked
        // rather than flicked at. Anything else commits whatever the pointer rests on - including
        // nothing, which simply closes the wheel.
        if (_timing.RealTime - _menuOpenedAt < TapLatchThreshold && _menu.PointerSelection == null)
        {
            _menu.DisablePointerMode();
            return;
        }

        _menu.CommitPointerSelection();
    }

    private void OpenMenu(bool pointerDriven)
    {
        _menu = new SimpleRadialMenu();
        // SS220-emote-wheel-rework-begin
        _menu.SetButtonPages(BuildSlots(), MenuSettings);

        // Reachable mid-round, right where the player is looking when they notice their wheel is wrong.
        // Only visible while click-driven, since during a hold there is no cursor to click it with.
        var editButton = new Button { Text = Loc.GetString("emote-wheel-editor-open") };
        editButton.OnPressed += _ => OpenEditor();
        _menu.SetBanner(editButton);
        // SS220-emote-wheel-rework-end

        _menu.OnClose += OnWindowClosed;
        _menu.OnOpen += OnWindowOpen;

        if (EmotesButton != null)
            EmotesButton.SetClickPressed(true);

        // Always screen-centred. Opening under the cursor is precisely how the old wheel ended up
        // unnoticed in a corner, and with the virtual pointer the real cursor position is irrelevant.
        _menu.OpenCentered();

        if (pointerDriven)
            _menu.EnablePointerMode();

        _menuOpenedAt = _timing.RealTime;
    }

    // SS220-emote-wheel-rework-end

    public void UnloadButton()
    {
        if (EmotesButton == null)
            return;

        EmotesButton.OnPressed -= ActionButtonPressed;
    }

    public void LoadButton()
    {
        if (EmotesButton == null)
            return;

        EmotesButton.OnPressed += ActionButtonPressed;
    }

    private void ActionButtonPressed(BaseButton.ButtonEventArgs args)
    {
        // SS220-emote-wheel-rework: the top bar button has no key to hold, so it always opens in
        // click-to-select mode.
        if (_menu != null)
            CloseMenu();
        else
            OpenMenu(pointerDriven: false);
    }

    private void OnWindowClosed()
    {
        if (EmotesButton != null)
            EmotesButton.Pressed = false;

        CloseMenu();
    }

    private void OnWindowOpen()
    {
        if (EmotesButton != null)
            EmotesButton.Pressed = true;
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        // SS220-emote-wheel-rework-begin
        // Detach and null out before disposing: closing now happens from inside the menu's own close
        // handling (commit on key release), so this can re-enter.
        var menu = _menu;
        _menu = null;

        menu.OnClose -= OnWindowClosed;
        menu.OnOpen -= OnWindowOpen;

        if (EmotesButton != null)
            EmotesButton.SetClickPressed(false);

        menu.Dispose();
        // SS220-emote-wheel-rework-end
    }

    /// <summary>
    /// Every emote the player can currently use, paired with the button model for it. Returns the
    /// prototype alongside the model so callers can match a saved arrangement against it by id.
    /// </summary>
    private List<(EmotePrototype Emote, RadialMenuOptionBase Option)> ConvertToButtons(
        IEnumerable<EmotePrototype> emotePrototypes
    ) // SS220-emote-wheel-rework
    {
        var whitelistSystem = EntitySystemManager.GetEntitySystem<EntityWhitelistSystem>();
        var player = _playerManager.LocalSession?.AttachedEntity;

        // SS220-emote-wheel-rework-begin
        // Flat, no category layer. The categories were never real categories - EmoteCategory.General is
        // byte.MaxValue, a flags mask that matches everything, which is why "General" ended up holding
        // farts and deathgasps alongside gestures. And a nesting layer costs an extra interaction on a
        // menu whose entire purpose is speed.
        //
        // The EmoteCategory enum itself stays: muting gates on Vocal, body emotes on Hands, and
        // EmoteBlockerComponent blocks by category. Only the UI grouping is gone.
        var models = new List<(EmotePrototype Emote, RadialMenuOptionBase Option)>();
        foreach (var emote in emotePrototypes)
        {
            // only valid emotes that have ways to be triggered by chat and player have access / no restriction on
            if (emote.Category == EmoteCategory.Invalid
                || emote.ChatTriggers.Count == 0
                || !(player.HasValue && whitelistSystem.IsWhitelistPassOrNull(emote.Whitelist, player.Value))
                || whitelistSystem.IsWhitelistPass(emote.Blacklist, player.Value))
                continue;

            if (!emote.Available
                && EntityManager.TryGetComponent<SpeechComponent>(player.Value, out var speech)
                && !speech.AllowedEmotes.Contains(emote.ID))
                continue;

            // The name goes on the button itself now. Burying it in a tooltip is what forced players to
            // either memorise every icon or wait out a hover delay on a menu built for speed.
            var name = Loc.GetString(emote.Name);
            models.Add((emote, new RadialMenuActionOption<EmotePrototype>(HandleRadialButtonClick, emote)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(emote.Icon),
                Label = name,
                ToolTip = BuildTooltip(name, emote)
            }));
        }

        // Prototype enumeration order is arbitrary, so sort by name for a stable default arrangement.
        models.Sort(static (a, b) =>
            string.Compare(a.Option.Label, b.Option.Label, StringComparison.CurrentCultureIgnoreCase));

        return models;
        // SS220-emote-wheel-rework-end
    }

    // SS220-emote-wheel-rework-begin
    /// <summary>
    /// Opens the wheel editor as a window. The wheel itself closes, since it is about to be rebuilt and
    /// would otherwise sit there showing a stale arrangement.
    /// </summary>
    private void OpenEditor()
    {
        CloseMenu();

        if (_editorWindow != null)
        {
            _editorWindow.MoveToFront();
            return;
        }

        // Without this the editor would load and save under the unkeyed default while the wheel reads the
        // player's species, so an in-round edit would quietly go to a wheel nothing reads.
        var editor = new EmoteWheelEditor();
        // Species and sex come from the body actually being played; the profile is only there to build
        // the preview character, so a mismatch between the two costs nothing more than a preview sprite.
        editor.SetCharacter(
            _preferences.Preferences?.SelectedCharacter as HumanoidCharacterProfile,
            GetPlayerSpecies(),
            GetPlayerSex());

        _editorWindow = new DefaultWindow
        {
            Title = Loc.GetString("emote-wheel-editor-title"),
            // Wide enough for the wheel, the character beside it and the emote list without squashing.
            MinSize = new Vector2(1020, 760),
        };

        _editorWindow.Contents.AddChild(editor);
        _editorWindow.OnClose += () => _editorWindow = null;
        _editorWindow.OpenCentered();
    }

    /// <summary>
    /// Species of the body being played, which is the key the wheel is stored under. Null when there is
    /// no body or it is not a humanoid, in which case the player gets the unkeyed default wheel.
    /// </summary>
    private string? GetPlayerSpecies()
    {
        var player = _playerManager.LocalSession?.AttachedEntity;

        return player.HasValue
            && EntityManager.TryGetComponent<HumanoidProfileComponent>(player.Value, out var humanoid)
                ? humanoid.Species.Id
                : null;
    }

    /// <summary> Sex of the body being played, used to preview the right voice. </summary>
    private Sex GetPlayerSex()
    {
        var player = _playerManager.LocalSession?.AttachedEntity;

        return player.HasValue
            && EntityManager.TryGetComponent<HumanoidProfileComponent>(player.Value, out var humanoid)
                ? humanoid.Sex
                : Sex.Unsexed;
    }

    /// <summary>
    /// Builds the wheel from the player's saved arrangement, falling back to whatever they can currently
    /// use if they have never configured it.
    /// </summary>
    private List<IEnumerable<RadialMenuOptionBase>> BuildSlots()
    {
        var available = ConvertToButtons(_prototypeManager.EnumeratePrototypes<EmotePrototype>());

        // Keyed by id so the saved arrangement can be resolved against what is actually usable right
        // now: an emote saved on a Tajaran is simply absent on a human rather than breaking the wheel.
        var usable = available.ToDictionary(static x => x.Emote.ID, static x => x.Option);

        var loadout = EmoteWheelLoadout.Load(_configuration, _prototypeManager, GetPlayerSpecies());
        if (loadout.IsEmpty)
            loadout = EmoteWheelLoadout.Default(available.Select(static x => new ProtoId<EmotePrototype>(x.Emote.ID)));

        var slots = new List<IEnumerable<RadialMenuOptionBase>>(EmoteWheelLoadout.SlotCount);
        foreach (var slot in loadout.Slots)
        {
            var options = new List<RadialMenuOptionBase>(EmoteWheelLoadout.SlotSize);
            foreach (var cell in slot)
            {
                if (cell.HasValue && usable.TryGetValue(cell.Value.Id, out var option))
                    options.Add(option);
            }

            // Empty slots are dropped rather than shown blank - scrolling onto nothing is worse than
            // simply having fewer slots.
            if (options.Count > 0)
                slots.Add(options);
        }

        if (slots.Count == 0)
            slots.Add(Array.Empty<RadialMenuOptionBase>());

        return slots;
    }

    /// <summary>
    /// The name is on the button now, so the tooltip is free to teach the chat command instead.
    /// </summary>
    private static string BuildTooltip(string name, EmotePrototype emote)
    {
        var trigger = emote.ChatTriggers.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(trigger))
            return name;

        return Loc.GetString("emote-menu-tooltip", ("name", name), ("trigger", (object) trigger));
    }
    // SS220-emote-wheel-rework-end

    private void HandleRadialButtonClick(EmotePrototype prototype)
    {
        EntityManager.RaisePredictiveEvent(new PlayEmoteMessage(prototype.ID));
    }
}
