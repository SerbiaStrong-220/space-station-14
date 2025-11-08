// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.CultYogg.Rave;

[RegisterComponent]
public sealed partial class RaveComponent : Component
{
    /// <summary>
    /// The minimum time in seconds between pronouncing rleh phrase.
    /// </summary>
    [DataField]
    public TimeSpan MinIntervalPhrase = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The maximum time in seconds between pronouncing rleh phrase.
    /// </summary>
    [DataField]
    public TimeSpan MaxIntervalPhrase = TimeSpan.FromSeconds(40);

    /// <summary>
    /// Buffer that contains next event
    /// </summary>
    public TimeSpan NextPhraseTime;

    public float SilentPhraseChance = 0.9f;

    /// <summary>
    /// The minimum time in seconds between playing the sound.
    /// </summary>
    [DataField]
    public TimeSpan MinIntervalSound = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The maximum time in seconds between playing the sound.
    /// </summary>
    [DataField]
    public TimeSpan MaxIntervalSound = TimeSpan.FromSeconds(35);

    /// <summary>
    /// Buffer that contains next event
    /// </summary>
    [DataField]
    public TimeSpan NextSoundTime;

    /// <summary>
    /// Contains phrases that player will pronounce
    /// </summary>
    [DataField]
    public string PhrasesPlaceholders = "CultRlehPhrases";

    /// <summary>
    /// Contains special sounds which be played during Rave
    /// </summary>
    [DataField]
    public SoundSpecifier RaveSoundCollection = new SoundCollectionSpecifier("RaveSounds");

    public EntityUid? EffectEntity = null;
}
