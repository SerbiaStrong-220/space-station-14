// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Bed.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.SS220.Virology;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    [Dependency] private SharedChatSystem _chat = default!;

    private void InitializeProgression()
    {
        SubscribeLocalEvent<VirusHolderComponent, ExaminedEvent>(OnExamined);
    }

    private void TickProgression()
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<VirusComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var virus = new Entity<VirusComponent>(uid, comp);
            var dead = _mobState.IsDead(comp.Carrier);

            if (comp.SuppressedUntil is { } until)
            {
                if (dead)
                {
                    comp.SuppressedUntil = until + UpdateInterval;
                    Dirty(virus);
                }
                else if (curTime >= until)
                {
                    ReactivateVirus(virus);
                }

                continue;
            }

            // Bank time strain shouldn't progres: supression, dead host, stasis bed.
            var banked = TimeSpan.Zero;
            if (dead)
            {
                banked = UpdateInterval;
            }
            else if (HasComp<StasisBedBuckledComponent>(comp.Carrier))
            {
                var multEv = new GetMetabolicMultiplierEvent();
                RaiseLocalEvent(comp.Carrier, ref multEv);
                if (multEv.Multiplier > 1f)
                    banked = UpdateInterval * (1d - 1d / multEv.Multiplier);
            }

            if (banked > TimeSpan.Zero)
            {
                foreach (var state in comp.Symptoms.Values)
                {
                    state.StageStartTime += banked;
                    state.LastEmote += banked;
                }

                Dirty(virus);
            }

            if (dead)
                continue;

            foreach (var (symptomId, state) in comp.Symptoms)
            {
                if (!_proto.Resolve(symptomId, out var symptom))
                    continue;

                TryAdvance(virus, symptomId, symptom, state, curTime);
                ApplyEffects(virus, symptom, state, curTime);
                TryManifest(virus, symptom, state, curTime);
            }
        }
    }

    private void TryAdvance(Entity<VirusComponent> virus, ProtoId<VirusSymptomPrototype> symptomId, VirusSymptomPrototype symptom, VirusSymptomState state, TimeSpan curTime)
    {
        if (state.Stage < 0 || state.Stage >= symptom.Stages.Length)
            return;

        var conditions = symptom.Stages[state.Stage].ProgressConditions;
        if (conditions.Length == 0 || state.Stage + 1 >= symptom.Stages.Length)
            return;

        var args = new VirusProgressArgs(virus.Owner, virus.Comp.Carrier, state, EntityManager, curTime, false);
        foreach (var condition in conditions)
        {
            if (!condition.CheckCondition(in args))
                return;
        }

        var oldStage = state.Stage;
        state.Stage++;
        state.StageStartTime = curTime;
        state.LastEmote = curTime;
        state.EmoteDelay = TimeSpan.Zero;
        ApplyStage(virus, symptomId, oldStage, state.Stage);
        Dirty(virus);
        RaiseContentsChanged(virus.Comp.Carrier);

        _adminLog.Add(LogType.Virology, LogImpact.Low,
            $"{ToPrettyString(virus.Comp.Carrier):target}: virus symptom {symptomId} advanced to stage {state.Stage + 1}");

        var newStage = symptom.Stages[state.Stage];
        if (newStage.ProgressMessage is { } message)
            VirusChat.SendSelfMessage(_chatManager, EntityManager, virus.Comp.Carrier, Loc.GetString(message), newStage.ProgressMessageColor);
    }

    private void ApplyEffects(Entity<VirusComponent> virus, VirusSymptomPrototype symptom, VirusSymptomState state, TimeSpan curTime)
    {
        var effects = BuildStageEffects(symptom, state.Stage, virus.Comp.Carrier);
        if (effects.Length == 0)
            return;

        var args = new VirusProgressArgs(virus.Owner, virus.Comp.Carrier, state, EntityManager, curTime, false);
        foreach (var effect in effects)
            effect.ApplyEffect(in args);
    }

    private void TryManifest(Entity<VirusComponent> virus, VirusSymptomPrototype symptom, VirusSymptomState state, TimeSpan curTime)
    {
        var manifest = ResolveManifestation(virus.Comp.Carrier, symptom, state.Stage);
        if (manifest == null || (manifest.Emote == null && manifest.EmoteMessage == null && manifest.SelfMessage == null))
            return;

        if (state.EmoteDelay <= TimeSpan.Zero)
        {
            state.EmoteDelay = manifest.EmoteIntervalMax is { } max && max > manifest.EmoteInterval
                ? manifest.EmoteInterval + (max - manifest.EmoteInterval) * _random.NextDouble()
                : manifest.EmoteInterval;
        }

        if (curTime < state.LastEmote + state.EmoteDelay)
            return;

        state.LastEmote = curTime;
        state.EmoteDelay = TimeSpan.Zero;

        if (!_random.Prob(manifest.EmoteChance))
            return;

        var carrier = virus.Comp.Carrier;

        if (manifest.Emote is { } emote)
            _chat.TryEmoteWithChat(carrier, emote);

        if (manifest.EmoteMessage is { } emoteMessage)
            _chat.TrySendInGameICMessage(carrier, Loc.GetString(emoteMessage), InGameICChatType.Emote, hideChat: false, hideLog: true);

        if (manifest.SelfMessage is { } selfMessage)
            VirusChat.SendSelfMessage(_chatManager, EntityManager, carrier, Loc.GetString(selfMessage), manifest.SelfMessageColor);
    }

    private void OnExamined(Entity<VirusHolderComponent> ent, ref ExaminedEvent args)
    {
        foreach (var strain in EnumerateStrains(ent.Comp))
        {
            var virus = strain.Comp;
            foreach (var (symptomId, state) in virus.Symptoms)
            {
                if (!_proto.Resolve(symptomId, out var symptom))
                    continue;

                var manifest = ResolveManifestation(ent.Owner, symptom, state.Stage);
                if (manifest is not { Visible: true } || manifest.ExamineText is not { } text)
                    continue;

                args.PushMarkup(Loc.GetString(text));
            }
        }
    }

    private VirusSymptomManifestation? ResolveManifestation(EntityUid carrier, VirusSymptomPrototype symptom, int stage)
    {
        if (stage < 0 || stage >= symptom.Stages.Length)
            return null;

        if (GetSpecies(carrier) is { } species
            && symptom.SpeciesOverrides.TryGetValue(species, out var over))
        {
            if (over.Immune || stage < over.MinStage)
                return null;

            if (over.ManifestationOverride is { } manifestationOverride)
                return manifestationOverride;
        }

        return symptom.Stages[stage].Manifestation;
    }
}
