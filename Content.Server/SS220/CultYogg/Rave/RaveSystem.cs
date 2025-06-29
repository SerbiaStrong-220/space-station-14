// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Shared.Dataset;
using Content.Shared.StatusEffect;
using Content.Shared.SS220.CultYogg.Rave;
using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Shared.Examine;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Rave;

public sealed class RaveSystem : SharedRaveSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaveComponent, ComponentStartup>(SetupRaving);

        SubscribeLocalEvent<RaveComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<RaveComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RaveComponent>();
        while (query.MoveNext(out var uid, out var raving))
        {
            if (raving.NextPhraseTime <= _timing.CurTime)
            {
                if (_random.Prob(raving.SilentPhraseChance))
                    _chat.TrySendInGameICMessage(uid, PickPhrase(raving.PhrasesPlaceholders), InGameICChatType.Whisper, ChatTransmitRange.Normal);
                else
                    _chat.TrySendInGameICMessage(uid, PickPhrase(raving.PhrasesPlaceholders), InGameICChatType.Speak, ChatTransmitRange.Normal);

                SetNextPhraseTimer(raving);
            }

            if (raving.NextSoundTime <= _timing.CurTime)
            {
                _audio.PlayEntity(raving.RaveSoundCollection, uid, uid);
                SetNextSoundTimer(raving);
            }
        }
    }

    private void OnSaintWaterDrinked(Entity<RaveComponent> uid, ref OnSaintWaterDrinkEvent args)
    {
        TryRemoveRavenness(uid);
    }

    private void OnExamined(Entity<RaveComponent> uid, ref ExaminedEvent args)
    {
        if (!HasComp<ShowCultYoggIconsComponent>(args.Examiner))
            return;

        args.PushMarkup($"[color=green]{Loc.GetString("cult-yogg-shroom-markup", ("ent", uid))}[/color]");
    }

    public void TryApplyRavenness(EntityUid uid, TimeSpan time, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, EffectKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<RaveComponent>(uid, EffectKey, time, true, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, EffectKey, time, status);
        }
    }

    public void TryRemoveRavenness(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, EffectKey);
    }

    public void TryRemoveRavenessTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, EffectKey, TimeSpan.FromSeconds(timeRemoved));
    }

    private void SetupRaving(Entity<RaveComponent> uid, ref ComponentStartup args)
    {
        SetNextPhraseTimer(uid.Comp);
        SetNextSoundTimer(uid.Comp);
    }

    private void SetNextPhraseTimer(RaveComponent comp)
    {
        comp.NextPhraseTime = _timing.CurTime + ((comp.MinIntervalPhrase < comp.MaxIntervalPhrase)
        ? _random.Next(comp.MinIntervalPhrase, comp.MaxIntervalPhrase)
        : comp.MaxIntervalPhrase);
    }

    private void SetNextSoundTimer(RaveComponent comp)
    {
        comp.NextSoundTime = _timing.CurTime + ((comp.MinIntervalSound < comp.MaxIntervalSound)
        ? _random.Next(comp.MinIntervalSound, comp.MaxIntervalSound)
        : comp.MaxIntervalSound);
    }

    private string PickPhrase(string name)
    {
        var dataset = _proto.Index<DatasetPrototype>(name);
        return _random.Pick(dataset.Values);
    }
}
