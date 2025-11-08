// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Content.Shared.SS220.EntityEffects.Events;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.CultYogg.Rave;

public abstract class SharedRaveSystem : EntitySystem
{
    private readonly EntProtoId _effectPrototype = "Rave";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectNew.StatusEffectsSystem _statusEffectsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaveComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RaveComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RaveComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinked);
    }

    private void OnStartup(Entity<RaveComponent> uid, ref ComponentStartup args)
    {
        SetNextPhraseTimer(uid.Comp);
        SetNextSoundTimer(uid.Comp);
    }

    private void OnExamined(Entity<RaveComponent> uid, ref ExaminedEvent args)
    {
        if (!HasComp<ShowCultYoggIconsComponent>(args.Examiner))
            return;

        args.PushMarkup($"[color=green]{Loc.GetString("cult-yogg-shroom-markup", ("ent", uid))}[/color]");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RaveComponent>();
        while (query.MoveNext(out var uid, out var raving))
        {
            if (raving.NextPhraseTime <= _timing.CurTime)
            {
                Mumble((uid, raving));

                SetNextPhraseTimer(raving);
            }

            if (raving.NextSoundTime > _timing.CurTime)
                continue;

            _audio.PlayEntity(raving.RaveSoundCollection, uid, uid);
            SetNextSoundTimer(raving);
        }
    }

    protected virtual void Mumble(Entity<RaveComponent> ent) { }

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

    private void OnSaintWaterDrinked(Entity<RaveComponent> uid, ref OnSaintWaterDrinkEvent args)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, _effectPrototype);
    }

}
