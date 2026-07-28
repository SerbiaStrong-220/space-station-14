using System.Linq;
using System.Numerics;
using Content.Client.Chat.UI;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Speech.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SS220.EmoteWheel;

/// <summary>
/// Lets the player choose which emotes sit on their wheel and where. Shared by the lobby character editor
/// and the in-round entry point, so both edit the same arrangement through the same UI.
/// </summary>
/// <remarks>
/// Editing happens on the wheel itself rather than on a separate grid: the player clicks the sector they
/// want to change, then the emote to put there. A grid alongside a preview meant reading the same
/// arrangement in two places and mentally mapping between them.
/// </remarks>
public sealed class EmoteWheelEditor : BoxContainer
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    private readonly BoxContainer _availableContainer;
    private readonly EmoteWheelPreview _preview;

    /// <summary>
    /// The character the wheel belongs to, shown the same way the lobby shows it. The bubble attaches to
    /// this, so a preview reads exactly like the emote will in game.
    /// </summary>
    private readonly ProfilePreviewSpriteView _character;
    private readonly Label _slotLabel;
    private readonly PageDots _slotDots;

    /// <summary> The live preview bubble, so a second preview replaces the first rather than stacking. </summary>
    private SpeechBubble? _bubble;

    private EmoteWheelLoadout _loadout = EmoteWheelLoadout.Empty();
    private int _selectedSlot;

    /// <summary> Sector being edited, or null when nothing is selected. </summary>
    private int? _selectedCell;

    /// <summary> Species the wheel is being edited for; decides both storage and what is greyed out. </summary>
    private string? _species;

    /// <summary> Sex of the character being configured, which picks the voice the preview plays. </summary>
    private Sex _sex = Sex.Unsexed;

    /// <summary> Body to render emote text against, for lines that use grammar. Null in the lobby. </summary>
    private EntityUid? _previewEntity;

    /// <summary> Ids usable by <see cref="_species"/>, or null for "no opinion, show everything". </summary>
    private HashSet<string>? _usable;

    public EmoteWheelEditor()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 12;
        VerticalExpand = true;
        HorizontalExpand = true;

        // Clicks that get this far were not claimed by any control, i.e. they landed on empty space
        // anywhere in the window, and those deselect too - not just empty space inside the wheel.
        MouseFilter = MouseFilterMode.Stop;

        // Left: the wheel, which is both the preview and the thing being edited.
        var left = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        var slotNav = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalAlignment = HAlignment.Center,
        };

        var previousButton = new Button { Text = "<" };
        previousButton.OnPressed += _ => StepSlot(-1);

        _slotLabel = new Label { VerticalAlignment = VAlignment.Center };

        var nextButton = new Button { Text = ">" };
        nextButton.OnPressed += _ => StepSlot(1);

        slotNav.AddChild(previousButton);
        slotNav.AddChild(_slotLabel);
        slotNav.AddChild(nextButton);
        left.AddChild(slotNav);

        // The same indicator the wheel itself uses, so the slot you are editing reads identically here.
        _slotDots = new PageDots
        {
            Count = EmoteWheelLoadout.SlotCount,
            HorizontalAlignment = HAlignment.Center,
        };
        left.AddChild(_slotDots);

        _character = new ProfilePreviewSpriteView
        {
            Scale = new Vector2(2f, 2f),
            OverrideDirection = Direction.South,
            VerticalAlignment = VAlignment.Center,
            MinSize = new Vector2(96f, 128f),
        };

        _preview = new EmoteWheelPreview { VerticalAlignment = VAlignment.Center };
        _preview.OnCellPressed += cell =>
        {
            // Clicking the selected sector again clears it, so deselecting never needs a precise miss.
            _selectedCell = _selectedCell == cell ? null : cell;
            RefreshAll();
        };
        _preview.OnBackgroundPressed += ClearSelection;
        _preview.OnCellRightPressed += cell =>
        {
            _loadout.Set(_selectedSlot, cell, null);
            Persist();
            RefreshAll();
        };
        // Wheel and character side by side, so the bubble has somewhere to sit that is not on top of the
        // thing being edited.
        left.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 12,
            HorizontalAlignment = HAlignment.Center,
            Children = { _preview, _character },
        });

        left.AddChild(new Label
        {
            Text = Loc.GetString("emote-wheel-editor-hint"),
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { StyleNano.StyleClassLabelSubText },
        });

        var actions = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalAlignment = HAlignment.Center,
        };

        var clearCellButton = new Button { Text = Loc.GetString("emote-wheel-editor-clear-cell") };
        clearCellButton.OnPressed += _ =>
        {
            if (_selectedCell is not { } cell)
                return;

            _loadout.Set(_selectedSlot, cell, null);
            Persist();
            RefreshAll();
        };

        var clearAllButton = new Button { Text = Loc.GetString("emote-wheel-editor-clear") };
        clearAllButton.OnPressed += _ =>
        {
            _loadout = EmoteWheelLoadout.Empty();
            Persist();
            RefreshAll();
        };

        var resetButton = new Button { Text = Loc.GetString("emote-wheel-editor-reset") };
        resetButton.OnPressed += _ =>
        {
            _loadout = BuildDefault();
            Persist();
            RefreshAll();
        };

        actions.AddChild(clearCellButton);
        actions.AddChild(clearAllButton);
        actions.AddChild(resetButton);
        left.AddChild(actions);

        AddChild(left);

        // Right: everything that can go on the wheel.
        var right = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            VerticalExpand = true,
            // Wide enough that longer emote names are not squeezed against the play button.
            MinWidth = 300,
        };

        right.AddChild(new Label { Text = Loc.GetString("emote-wheel-editor-available") });

        _availableContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        right.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _availableContainer },
        });

        AddChild(right);

        Reload();
    }

    /// <summary>
    /// Points the editor at the character being configured. Species decides which wheel is edited and what
    /// is greyed out; sex picks the voice the preview plays; the entity, where there is one, lets emote
    /// text render with the right grammar.
    /// </summary>
    public void SetCharacter(HumanoidCharacterProfile? profile, string? species, Sex sex)
    {
        // Rebuilding the dummy is expensive, so only do it when the character actually changed.
        var changed = _species != species || _sex != sex || _usable == null;

        if (profile != null && (changed || _previewEntity == null))
        {
            _character.LoadPreview(profile);
            _previewEntity = _character.PreviewDummy;
        }

        if (!changed)
            return;

        _species = species;
        _sex = sex;
        Reload();
    }

    /// <inheritdoc />
    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        ClearSelection();
        args.Handle();
    }

    private void ClearSelection()
    {
        if (_selectedCell == null)
            return;

        _selectedCell = null;
        RefreshAll();
    }

    private void Reload()
    {
        _usable = EmoteAvailability.ForSpecies(_prototypes, _species);

        // Presented pre-filled with the default rather than blank: an empty wheel gives the player nothing
        // to reason about, and the wheel they actually have is the only sensible starting point.
        var loaded = EmoteWheelLoadout.Load(_cfg, _prototypes, _species);
        _loadout = loaded.IsEmpty ? BuildDefault() : loaded;

        RefreshAll();
    }

    /// <summary>
    /// The arrangement a player gets before configuring anything: usable emotes by name, as many as fit.
    /// </summary>
    private EmoteWheelLoadout BuildDefault()
    {
        return EmoteWheelLoadout.Default(
            SelectableEmotes().Where(IsUsable).Select(static x => new ProtoId<EmotePrototype>(x.ID)));
    }

    /// <summary> True when the current species can actually use this emote. </summary>
    private bool IsUsable(EmotePrototype emote) => _usable == null || _usable.Contains(emote.ID);

    private void RefreshAll()
    {
        RebuildAvailable();
        RefreshPreview();
    }

    private void StepSlot(int delta)
    {
        _selectedSlot = (_selectedSlot + delta + EmoteWheelLoadout.SlotCount) % EmoteWheelLoadout.SlotCount;

        // The selection belonged to the slot we just left.
        _selectedCell = null;
        RefreshAll();
    }

    private void RefreshPreview()
    {
        _preview.SelectedCell = _selectedCell;
        _preview.Update(_loadout.Slots[_selectedSlot]);
        _slotLabel.Text = Loc.GetString("emote-wheel-editor-slot", ("index", _selectedSlot + 1));
        _slotDots.Active = _selectedSlot;
    }

    /// <summary>
    /// Every emote that can meaningfully go on a wheel, usable ones first so the list opens on what the
    /// character can actually do.
    /// </summary>
    private IEnumerable<EmotePrototype> SelectableEmotes()
    {
        return _prototypes.EnumeratePrototypes<EmotePrototype>()
            .Where(static x => x.Category != EmoteCategory.Invalid && x.ChatTriggers.Count > 0)
            .OrderByDescending(IsUsable)
            .ThenBy(static x => Loc.GetString(x.Name), StringComparer.CurrentCultureIgnoreCase);
    }

    private void RebuildAvailable()
    {
        _availableContainer.RemoveAllChildren();

        foreach (var emote in SelectableEmotes())
        {
            var usable = IsUsable(emote);

            var content = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 6,
                HorizontalExpand = true,
            };
            content.AddChild(BuildIcon(emote));
            content.AddChild(new Label
            {
                Text = Loc.GetString(emote.Name),
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
            });

            // Two different reasons to be unclickable, kept visually distinct: a restricted emote is
            // greyed because it can never go on this character's wheel, whereas "no sector picked yet" is
            // a passing state and keeps its normal colour.
            var awaitingSector = _selectedCell == null;

            var assignButton = new Button
            {
                Disabled = !usable || awaitingSector,
                ToolTip = !usable
                    ? Loc.GetString("emote-wheel-editor-unavailable")
                    : awaitingSector
                        ? Loc.GetString("emote-wheel-editor-pick-sector")
                        : null,
                HorizontalExpand = true,
                Children = { content },
                Modulate = usable ? Color.White : new Color(110, 110, 110),
            };

            var id = emote.ID;
            assignButton.OnPressed += _ =>
            {
                if (_selectedCell is not { } cell)
                    return;

                _loadout.Set(_selectedSlot, cell, id);
                Persist();
                RefreshAll();
            };

            // Its own fixed-width button: sharing the row button made it stretch with the name length.
            var playButton = new Button
            {
                Text = "▶",
                MinWidth = 34f,
                ToolTip = Loc.GetString("emote-wheel-editor-preview-tooltip"),
            };
            playButton.OnPressed += _ => PreviewEmote(emote, playButton);

            _availableContainer.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 2,
                Children = { assignButton, playButton },
            });
        }
    }

    private Control BuildIcon(EmotePrototype emote)
    {
        return new TextureRect
        {
            Texture = _entities.System<SpriteSystem>().Frame0(emote.Icon),
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            Stretch = TextureRect.StretchMode.KeepCentered,
        };
    }

    /// <summary>
    /// Plays what the emote sounds like for this character and shows the line others would read, using the
    /// game's own speech bubble placed where the player clicked. Entirely local - nothing is sent, so
    /// previewing does not emote at people.
    /// </summary>
    private void PreviewEmote(EmotePrototype emote, Control source)
    {
        // One bubble at a time, so repeated previews replace rather than pile up.
        _bubble?.Dispose();

        var text = BuildEmoteMessage(emote);
        var message = new ChatMessage(ChatChannel.Emotes, text, text, NetEntity.Invalid, null);

        // The real bubble, detached so it stays where it is put instead of tracking a body through the
        // world - the editor's character is a UI dummy, not something on the map.
        // SpeechBubble.Die only raises OnDied; removing the control is the caller's job, so without this
        // the bubble would sit on screen forever once its timer ran out.
        var bubble = new TextSpeechBubble(message, EntityUid.Invalid, "emoteBox") { Detached = true };
        bubble.OnDied += (_, died) =>
        {
            if (_bubble == died)
                _bubble = null;

            died.Dispose();
        };

        _bubble = bubble;
        _userInterface.PopupRoot.AddChild(bubble);

        // Above the character's head, the way it appears in game.
        bubble.Measure(Vector2Helpers.Infinity);
        var anchor = _character.GlobalPosition;
        LayoutContainer.SetPosition(bubble, new Vector2(
            anchor.X + (_character.Size.X - bubble.DesiredSize.X) * 0.5f,
            anchor.Y - bubble.DesiredSize.Y - 4f));

        if (ResolveEmoteSound(emote) is { } sound)
            _entities.System<SharedAudioSystem>().PlayGlobal(sound, Filter.Local(), false);
    }

    /// <summary>
    /// The line others would read, wrapped exactly as the chat system wraps an emote so the bubble shows
    /// the real thing - italics, name and all - rather than a bare sentence.
    /// </summary>
    private string BuildEmoteMessage(EmotePrototype emote)
    {
        var body = emote.ChatMessages.Count == 0
            ? Loc.GetString("emote-wheel-editor-preview-silent")
            : ResolveEmoteLine(emote.ChatMessages[0]);

        if (_previewEntity is not { } entity || !_entities.EntityExists(entity))
            return $"[italic]{body}[/italic]";

        return Loc.GetString(
            "chat-manager-entity-me-wrap-message",
            ("entity", entity),
            ("entityName", Identity.Name(entity, _entities)),
            ("message", body));
    }

    /// <summary> An emote's own line, which may run the actor through grammar functions. </summary>
    private string ResolveEmoteLine(string key)
    {
        if (_previewEntity is { } entity
            && _entities.EntityExists(entity)
            && Loc.TryGetString(key, out var withEntity, ("entity", entity)))
        {
            return withEntity;
        }

        return Loc.TryGetString(key, out var plain) ? plain : key;
    }

    /// <summary> The sound this character makes for the emote, if any. </summary>
    private SoundSpecifier? ResolveEmoteSound(EmotePrototype emote)
    {
        if (ResolveVocalSounds() is { } vocal && vocal.Sounds.TryGetValue(emote.ID, out var vocalSound))
            return vocalSound;

        // Hand emotes such as claps and salutes live in a separate collection held by a server-only
        // component, which the client cannot read off the prototype. Falling back to whichever collection
        // defines the emote covers them without inventing a shared component just for a preview.
        foreach (var collection in _prototypes.EnumeratePrototypes<EmoteSoundsPrototype>())
        {
            if (collection.Sounds.TryGetValue(emote.ID, out var sound))
                return sound;
        }

        return null;
    }

    private EmoteSoundsPrototype? ResolveVocalSounds()
    {
        if (_species == null
            || !_prototypes.TryIndex<SpeciesPrototype>(_species, out var species)
            || !_prototypes.TryIndex(species.Prototype, out var entity)
            || !entity.TryGetComponent<VocalComponent>("Vocal", out var vocal))
        {
            return null;
        }

        var collectionId = vocal.EmoteSounds;
        if (collectionId == null && vocal.Sounds != null)
        {
            // Voices are per sex, so preview the one this character will actually have. Fall back to any
            // entry if the species has no voice for that sex rather than previewing silence.
            if (!vocal.Sounds.TryGetValue(_sex, out var bySex))
            {
                foreach (var (_, id) in vocal.Sounds)
                {
                    bySex = id;
                    break;
                }
            }

            collectionId = bySex;
        }

        return collectionId != null && _prototypes.TryIndex(collectionId.Value, out var collection)
            ? collection
            : null;
    }

    private void Persist()
    {
        _loadout.Save(_cfg, _species);
    }
}
