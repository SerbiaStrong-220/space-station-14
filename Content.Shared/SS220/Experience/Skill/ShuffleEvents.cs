// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Experience.Skill;

public abstract class UiAnalyzerShuffleChance(float shuffleChance = 0f) : EntityEventArgs
{
    public float ShuffleChance = shuffleChance;
}

public sealed class GetHealthAnalyzerShuffleChance(float shuffleChance = 0f) : UiAnalyzerShuffleChance(shuffleChance);
