// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Content.Shared.Destructible.Thresholds;

namespace Content.Shared.SS220.CultYogg.Cultists;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CultYoggPurifiedComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 TotalAmountOfHolyWater = 0;

    [DataField]
    public FixedPoint2 AmountToPurify = 10;

    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    public MinMax TimeBetweenIncidents = new(0, 5); //ToDo maybe add some damage or screams? should discuss

    /// <summary>
    /// Buffer to markup when time has come
    /// </summary>
    [DataField]
    public TimeSpan? PurifyingDecayEventTime;

    /// <summary>
    /// Contains special sounds which be played when entity will be purified
    /// </summary>
    [DataField]
    public SoundSpecifier PurifyingCollection = new SoundCollectionSpecifier("CultYoggPurifyingSounds");

    [DataField("sprite")]
    public SpriteSpecifier.Rsi Sprite = new(new("SS220/Effects/cult_yogg_purifying.rsi"), "purifyingEffect");

    /// <summary>
    /// Amount of time requierd to requied for purifying removal
    /// </summary>
    [DataField]
    public TimeSpan BeforeDeclinesTime = TimeSpan.FromSeconds(120);
}
