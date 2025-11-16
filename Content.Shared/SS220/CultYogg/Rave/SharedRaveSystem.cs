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
    private readonly EntProtoId _statusEffectPrototype = "Rave";

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

    private void OnStartup(Entity<RaveComponent> ent, ref ComponentStartup args)
    {
        SetNextPhraseTimer(ent);
        SetNextSoundTimer(ent);
    }

    private void OnExamined(Entity<RaveComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<ShowCultYoggIconsComponent>(args.Examiner))
            return;

        args.PushMarkup($"[color=green]{Loc.GetString("cult-yogg-shroom-markup", ("ent", ent))}[/color]");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RaveComponent>();
        while (query.MoveNext(out var ent, out var raving))
        {
            if (raving.NextPhraseTime <= _timing.CurTime)
            {
                Mumble((ent, raving));

                SetNextPhraseTimer((ent, raving));
            }

            if (raving.NextSoundTime > _timing.CurTime)
                continue;

            _audio.PlayLocal(raving.RaveSoundCollection, ent, ent);
            SetNextSoundTimer((ent, raving));
        }
    }

    protected virtual void Mumble(Entity<RaveComponent> ent) { }

    private void SetNextPhraseTimer(Entity<RaveComponent> ent)
    {
        ent.Comp.NextPhraseTime = _timing.CurTime + ((ent.Comp.MinIntervalPhrase < ent.Comp.MaxIntervalPhrase)
        ? _random.Next(ent.Comp.MinIntervalPhrase, ent.Comp.MaxIntervalPhrase)
        : ent.Comp.MaxIntervalPhrase);

        Dirty(ent, ent.Comp);
    }

    private void SetNextSoundTimer(Entity<RaveComponent> ent)
    {
        TimeSpan randomTime;
        if (ent.Comp.MinIntervalSound < ent.Comp.MaxIntervalSound)
            randomTime = _random.Next(ent.Comp.MinIntervalSound, ent.Comp.MaxIntervalSound);
        else
            randomTime = ent.Comp.MaxIntervalSound;

        ent.Comp.NextSoundTime = _timing.CurTime + randomTime;

        Dirty(ent, ent.Comp);
    }

    private void OnSaintWaterDrinked(Entity<RaveComponent> uid, ref OnSaintWaterDrinkEvent args)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, _statusEffectPrototype);//ToDo_SS220 it isn't working cause can't find status Entity
    }
}
