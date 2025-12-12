// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Experience.Skill;

public abstract class ShuffleChanceGetterEvent : EntityEventArgs
{
    public float ShuffleChance = 0;
}

public sealed class GetHealthAnalyzerShuffleChance : ShuffleChanceGetterEvent;
