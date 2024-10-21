// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.SS220.CultYogg.Components;

namespace Content.Server.SS220.CultYogg;

[RegisterComponent, NetworkedComponent]
public sealed partial class RaveComponent : SharedRaveComponent
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
    public TimeSpan NextSoundTime;

    /// <summary>
    /// Contains special sounds which be played during Rave
    /// </summary>
    [DataField]
    public SoundSpecifier RaveSoundCollection = new SoundCollectionSpecifier("RaveSounds");
}
