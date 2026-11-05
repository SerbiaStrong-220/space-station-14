// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Experience.Skill;

/// <summary>
/// Event for getting shuffle chance, raised on entity which shuffle chance we want
/// </summary>
public abstract class ShuffleChanceGetterEvent : EntityEventArgs
{
    public float ShuffleChance = 0;
}

/// <summary> <inheritdoc/> </summary>
[ByRefEvent]
public sealed class GetHealthAnalyzerShuffleChance : ShuffleChanceGetterEvent;
