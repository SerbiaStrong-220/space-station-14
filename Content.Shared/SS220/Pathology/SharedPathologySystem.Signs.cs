// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    [Dependency] private SharedChatSystem _chat = default!;

    private void InitializeSigns()
    {
        SubscribeLocalEvent<PathologyHolderComponent, ExaminedEvent>(OnExamined);
    }

    public SymptomManifestation? ResolveManifestation(EntityUid holder, PathologyDefinition stage)
    {
        if (TryComp<HumanoidProfileComponent>(holder, out var humanoid)
            && stage.SpeciesManifestations.TryGetValue(humanoid.Species, out var speciesManifest))
            return speciesManifest;

        return stage.Manifestation;
    }

    private void OnExamined(Entity<PathologyHolderComponent> entity, ref ExaminedEvent args)
    {
        foreach (var (protoId, data) in entity.Comp.ActivePathologies)
        {
            if (!_prototype.Resolve(protoId, out var symptom))
                continue;

            if (data.Level >= symptom.Definition.Length)
                continue;

            var manifest = ResolveManifestation(entity, symptom.Definition[data.Level]);
            if (manifest is not { Visible: true } || manifest.ExamineText is not { } text)
                continue;

            args.PushMarkup(Loc.GetString(text));
        }
    }

    private void TryDoSymptomEmote(Entity<PathologyHolderComponent> entity, PathologyPrototype symptom, PathologyInstanceData data)
    {
        // client prediction would double emotes
        if (_net.IsClient)
            return;

        if (data.Level >= symptom.Definition.Length)
            return;

        var manifest = ResolveManifestation(entity, symptom.Definition[data.Level]);
        if (manifest == null || (manifest.Emote == null && manifest.EmoteMessage == null && manifest.SelfMessage == null))
            return;

        if (_gameTiming.CurTime < data.LastEmote + manifest.EmoteInterval)
            return;
        data.LastEmote = _gameTiming.CurTime;

        if (!_random.Prob(manifest.EmoteChance))
            return;

        if (manifest.Emote is { } emote)
            _chat.TryEmoteWithChat(entity, emote);

        if (manifest.EmoteMessage is { } emoteMessage)
            _chat.TrySendInGameICMessage(entity, Loc.GetString(emoteMessage), InGameICChatType.Emote, hideChat: false, hideLog: true);

        if (manifest.SelfMessage is { } selfMessage)
            SendSelfMessage(entity, Loc.GetString(selfMessage), manifest.SelfMessageColor);
    }

    protected virtual void SendSelfMessage(EntityUid entity, string message, Color? color) { }

    /// <summary>
    /// The scanner-facing description of a symptom (the first stage's <see cref="SymptomDetection.Description"/>).
    /// False if the symptom has no detectable stage. Shared by the diagnoser and vaccinator UIs.
    /// </summary>
    public bool TryGetSymptomDescription(ProtoId<PathologyPrototype> symptomId, out string? description)
    {
        description = null;
        if (!_prototype.Resolve(symptomId, out var symptom))
            return false;

        foreach (var stage in symptom.Definition)
        {
            if (stage.Detection is not { } detection)
                continue;

            description = detection.Description is { } desc ? Loc.GetString(desc) : null;
            return true;
        }

        return false;
    }

    public string FormatSymptom(ProtoId<PathologyPrototype> symptomId, string description, VirusInstance virus, bool showAccelerant = false)
    {
        if (!_prototype.Resolve(symptomId, out var symptom) || symptom.Definition.Length <= 1)
            return description;

        var stage = virus.SymptomStages.GetValueOrDefault(symptomId) + 1;

        if (showAccelerant
            && virus.Accelerants.TryGetValue(symptomId, out var accelerant)
            && _prototype.Resolve(accelerant, out var accelerantProto))
        {
            return Loc.GetString("disease-diagnoser-symptom-stage",
                ("symptom", description),
                ("stage", stage),
                ("max", symptom.Definition.Length),
                ("reagent", accelerantProto.LocalizedName));
        }

        return Loc.GetString("pathology-symptom-stage",
            ("symptom", description),
            ("stage", stage),
            ("max", symptom.Definition.Length));
    }
}
