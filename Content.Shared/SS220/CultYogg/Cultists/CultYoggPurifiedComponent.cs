// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.CultYogg.Cultists;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CultYoggPurifiedComponent : Component
{
    /// <summary>
    /// Holy water buffer
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 TotalAmountOfHolyWater = 0;

    /// <summary>
    /// The amount of holy water in units required for deconversion
    /// </summary>
    [DataField]
    public FixedPoint2 AmountToPurify = 30;

    /// <summary>
    /// Amount of time requierd to requied for purifying removal
    /// </summary>
    [DataField]
    public TimeSpan BeforeDecayTime = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Buffer to markup when time to decrease Holy water buffer has come
    /// </summary>
    public TimeSpan? DecayTime;

    /// <summary>
    /// The time it takes for the cultist to purify itself is needed to cancel it, if the cultist has the opportunity
    /// </summary>
    [DataField]
    public TimeSpan BeforePurifyingTime = TimeSpan.FromSeconds(120);

    /// <summary>
    /// The exact time when the cultist will be purified
    /// </summary>
    public TimeSpan? PurifyTime;

    /// <summary>
    /// Contains special sounds which be played when entity will be purified
    /// </summary>
    [DataField]
    public SoundSpecifier PurifiedSound = new SoundCollectionSpecifier("CultYoggPurifyingSounds");

    [DataField]
    public SpriteSpecifier.Rsi Sprite = new(new("SS220/Effects/CultYogg/purifying.rsi"), "purifyingEffect");
}
