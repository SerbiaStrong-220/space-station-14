// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.SS220.CultYogg.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultYoggCleansedComponent : Component
{
    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    public Vector2 TimeBetweenIncidents = new Vector2(0, 5); //ToDo maybe add some damage or screams? should discuss

    /// <summary>
    /// Buffer to markup when time has come
    /// </summary>
    [DataField]
    public TimeSpan? CleansingDecayEventTime;

    /// <summary>
    /// Amount of time requierd to requied for cleansind removal
    /// </summary>
    [DataField]
    public TimeSpan BeforeDeclinesTime = TimeSpan.FromSeconds(500);

    public FixedPoint2 AmountOfHolyWater = 0;

    public FixedPoint2 AmountToCleance = 10;
}
