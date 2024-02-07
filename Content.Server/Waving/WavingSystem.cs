using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Content.Shared.Waving;
using Robust.Shared.Prototypes;

namespace Content.Server.Waving;

/// <summary>
/// Adds an action to toggle waving animation for tails markings that supporting this
/// </summary>
public sealed class WavingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WavingComponent, MapInitEvent>(OnWavingMapInit);
        SubscribeLocalEvent<WavingComponent, ComponentShutdown>(OnWavingShutdown);
        SubscribeLocalEvent<WavingComponent, ToggleActionEvent>(OnWavingToggle);
        SubscribeLocalEvent<WavingComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnWavingMapInit(EntityUid uid, WavingComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    private void OnWavingShutdown(EntityUid uid, WavingComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnWavingToggle(EntityUid uid, WavingComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleWaving(uid, waving: component);
    }

    private void OnMobStateChanged(EntityUid uid, WavingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (component.Waving)
            TryToggleWaving(uid, waving: component);
    }

    public bool TryToggleWaving(EntityUid uid, WavingComponent? waving = null, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref waving, ref humanoid))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            return false;

        if (markings.Count == 0)
            return false;

        waving.Waving = !waving.Waving;

        for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails
        {
            var currentMarkingId = markings[idx].MarkingId;
            string newMarkingId;

            if (waving.Waving)
            {
                newMarkingId = $"{currentMarkingId}{waving.Suffix}";
            }
            else
            {
                if (currentMarkingId.EndsWith(waving.Suffix))
                {
                    newMarkingId = currentMarkingId[..^waving.Suffix.Length];
                }
                else
                {
                    newMarkingId = currentMarkingId;
                    Log.Warning($"Unable to revert waving for {currentMarkingId}");
                }
            }

            if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
            {
                Log.Warning($"{ToPrettyString(uid)} tried toggling waving but {newMarkingId} marking doesn't exist");
                continue;
            }

            _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, idx, newMarkingId,
                humanoid: humanoid);
        }

        var emoteText = Loc.GetString(waving.Waving ? "waving-emote-start" : "waving-emote-stop", ("ent", uid));
        _chat.TrySendInGameICMessage(uid, emoteText, InGameICChatType.Emote, ChatTransmitRange.Normal); // Ok while emotes dont have radial menu

        return true;
    }
}
