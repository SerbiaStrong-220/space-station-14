using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.SS220.Speech;// SS220 Chat-Special-Emote

namespace Content.Server.Speech.EntitySystems;

public sealed class VocalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IEntityManager _entities = default!;// SS220 Chat-Special-Emote

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VocalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VocalComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VocalComponent, SexChangedEvent>(OnSexChanged);
        SubscribeLocalEvent<VocalComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<VocalComponent, ScreamActionEvent>(OnScreamAction);
        SubscribeLocalEvent<VocalComponent, InitSpecialSoundsEvent>(InitSpecialSounds);// SS220 Chat-Special-Emote
        SubscribeLocalEvent<VocalComponent, UnloadSpecialSoundsEvent>(UnloadSpecialSounds);// SS220 Chat-Special-Emote
    }

    /// <summary>
    /// Copy this component's datafields from one entity to another.
    /// This can't use CopyComp because of the ScreamActionEntity DataField, which should not be copied.
    /// <summary>
    public void CopyComponent(Entity<VocalComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp))
            return;

        var targetComp = EnsureComp<VocalComponent>(target);
        targetComp.Sounds = source.Comp.Sounds;
        targetComp.ScreamId = source.Comp.ScreamId;
        targetComp.Wilhelm = source.Comp.Wilhelm;
        targetComp.WilhelmProbability = source.Comp.WilhelmProbability;
        LoadSounds(target, targetComp);

        Dirty(target, targetComp);
    }

    private void OnMapInit(EntityUid uid, VocalComponent component, MapInitEvent args)
    {
        // try to add scream action when vocal comp added
        _actions.AddAction(uid, ref component.ScreamActionEntity, component.ScreamAction);
        LoadSounds(uid, component);
    }

    private void OnShutdown(EntityUid uid, VocalComponent component, ComponentShutdown args)
    {
        // remove scream action when component removed
        if (component.ScreamActionEntity != null)
        {
            _actions.RemoveAction(uid, component.ScreamActionEntity);
        }
    }

    private void OnSexChanged(EntityUid uid, VocalComponent component, SexChangedEvent args)
    {
        LoadSounds(uid, component, args.NewSex);
    }

    private void OnEmote(EntityUid uid, VocalComponent component, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        // SS220 Chat-Special-Emote begin
        //Will play special emote if it exists
        if(CheckSpecialSounds(uid, component, args.Emote))
        {
            args.Handled = true;
            return;
        }
        // SS220 Chat-Special-Emote end

        // snowflake case for wilhelm scream easter egg
        if (args.Emote.ID == component.ScreamId)
        {
            args.Handled = TryPlayScreamSound(uid, component);
            return;
        }

        if (component.EmoteSounds is not { } sounds)
            return;

        // just play regular sound based on emote proto
        args.Handled = _chat.TryPlayEmoteSound(uid, _proto.Index(sounds), args.Emote);
    }

    private void OnScreamAction(EntityUid uid, VocalComponent component, ScreamActionEvent args)
    {
        if (args.Handled)
            return;

        _chat.TryEmoteWithChat(uid, component.ScreamId);
        args.Handled = true;
    }

    private bool TryPlayScreamSound(EntityUid uid, VocalComponent component)
    {
        if (_random.Prob(component.WilhelmProbability))
        {
            _audio.PlayPvs(component.Wilhelm, uid, component.Wilhelm.Params);
            return true;
        }

        if (component.EmoteSounds is not { } sounds)
            return false;

        return _chat.TryPlayEmoteSound(uid, _proto.Index(sounds), component.ScreamId);
    }

    private void LoadSounds(EntityUid uid, VocalComponent component, Sex? sex = null)
    {
        if (component.Sounds == null)
            return;

        sex ??= CompOrNull<HumanoidAppearanceComponent>(uid)?.Sex ?? Sex.Unsexed;

        if (!component.Sounds.TryGetValue(sex.Value, out var protoId))
            return;

        if (!_proto.HasIndex(protoId))
            return;

        component.EmoteSounds = protoId;
    }

    // SS220 Chat-Special-Emote begin
    private bool CheckSpecialSounds(EntityUid uid, VocalComponent component, EmotePrototype emote)
    {
        if (component.SpecialEmoteSounds == null)
            return false;

        foreach (var specEmote in component.SpecialEmoteSounds)
            if (_chat.TryPlayEmoteSound(uid, specEmote.Value, emote))
                return true;

        return false;
    }
    private void LoadSpecialSounds(EntityUid uid, VocalComponent component, EntityUid itemUid, VocalComponent itemComponent, Sex? sex = null)
    {
        if (itemComponent.Sounds == null)
            return;

        if (component.SpecialEmoteSounds == null)
            component.SpecialEmoteSounds = new();

        sex ??= CompOrNull<HumanoidAppearanceComponent>(uid)?.Sex ?? Sex.Unsexed;

        if (!itemComponent.Sounds.TryGetValue(sex.Value, out var protoId))
            return;

        itemComponent.EmoteSounds = protoId;

        if (!_proto.TryIndex(protoId, out var emoteProto))
        {
            Log.Error($"Cant index proto {protoId} for usage in special emotes");
            return;
        }

        component.SpecialEmoteSounds.Add(itemUid, emoteProto);
    }
    private void InitSpecialSounds(EntityUid uid, VocalComponent component, InitSpecialSoundsEvent args)
    {
        _entities.TryGetComponent<VocalComponent>(args.Item, out var itemComponent);

        if (itemComponent == null)
            return;

        if (component.SpecialEmoteSounds != null && component.SpecialEmoteSounds.ContainsKey(args.Item))
            return;

        LoadSpecialSounds(uid, component, args.Item, itemComponent);
    }
    private void UnloadSpecialSounds(EntityUid uid, VocalComponent component, UnloadSpecialSoundsEvent args)
    {
        if (component.SpecialEmoteSounds == null)
            return;

        component.SpecialEmoteSounds.Remove(args.Item);

        if (component.SpecialEmoteSounds.Count < 1)
            component.SpecialEmoteSounds = null;
    }
    // SS220 Chat-Special-Emote end
}
