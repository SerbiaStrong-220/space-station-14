// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Grab;

// numeration is important for stages upgrade
public enum GrabStage
{
    None = 0,
    Passive = 1,
    Aggressive = 2,
    NeckGrab = 3,
    Chokehold = 4,
    Last = 4, // if further stages will be added be sure to change the "Last" entry
}
