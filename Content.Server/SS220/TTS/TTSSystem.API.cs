// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid;
using Content.Shared.SS220.TTS;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    /// <summary>
    /// Adds <see cref="TTSComponent"/>  if entity don't have it
    /// </summary>
    public bool TrySetTTS(Entity<TTSComponent?> entity, ProtoId<TTSVoicePrototype>? voice)
    {
        if (!CanHaveVoice(entity.Owner, voice))
            return false;

        // in case if somehow we end in incorrect id -> log it
        if (!_prototypeManager.Resolve(voice, out _))
            return false;

        entity.Comp = EnsureComp<TTSComponent>(entity.Owner);
        entity.Comp.VoicePrototypeId = voice;
        return true;
    }

    public bool TryChangeTTS(Entity<TTSComponent?> entity, ProtoId<TTSVoicePrototype>? voice)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!CanHaveVoice(entity.Owner, voice))
            return false;

        // in case if somehow we end in incorrect id -> log it
        if (!_prototypeManager.Resolve(voice, out _))
            return false;

        entity.Comp.VoicePrototypeId = voice;
        return true;
    }

    // TODO: this needs to be moved to shared and used every where
    public bool CanHaveVoice(Entity<HumanoidProfileComponent?> entity, ProtoId<TTSVoicePrototype>? voice)
    {
        // no error message cause it can be user input
        if (!_prototypeManager.TryIndex(voice, out var voicePrototype))
            return false;

        return CanHaveVoice(entity, voicePrototype);
    }

    public bool CanHaveVoice(Entity<HumanoidProfileComponent?> entity, TTSVoicePrototype? voicePrototype)
    {
        if (voicePrototype is null)
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        return voicePrototype.Sex == Sex.Unsexed || entity.Comp.Sex == Sex.Unsexed || voicePrototype.Sex == entity.Comp.Sex;
    }
}

