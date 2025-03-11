namespace Content.Server.SS220.Tarot;

[RegisterComponent]
public sealed partial class TarotColodeComponent : Component
{
    [DataField]
    public TimeSpan? NextUseTime;

    [DataField]
    public HashSet<string> CardsName = new()
    {
        "TarotFoolCard",
        "TarotMagicianCard",
        "TarotHighPriestessCard",
        "TarotEmpressCard",
        "TarotHierophantCard",
        "TarotLoversCard",
        "TarotJusticeCard",
        "TarotHermitCard",
        "TarotWheelOfFortuneCard",
        "TarotStrengthCard",
        "TarotHangedManCard",
        "TarotDeathCard",
        "TarotTemperanceCard",
        "TarotDevilCard",
        "TarotTowerCard",
        "TarotStarsCard",
        "TarotMoonCard",
        "TarotSunCard",
        "TarotJudgementCard",
        "TarotWorldCard",
    };
}
