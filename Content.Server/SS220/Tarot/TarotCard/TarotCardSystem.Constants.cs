namespace Content.Server.SS220.Tarot.TarotCard;

/// <summary>
/// Contains constants and list, if applicable
/// </summary>
public sealed partial class TarotCardSystem
{
    private const string TarotCardEffectPrototype = "EffectTarotCard";

    private readonly List<string> _headsOfDepartment =
    [
        "HeadOfPersonnel",
        "HeadOfSecurity",
        "ChiefEngineer",
        "ResearchDirector",
        "ChiefMedicalOfficer",
        "Quartermaster",
        "Captain",
    ];

    private const string StatusEffectPacifism = "StatusEffectPacifism";

    private const string Chemicals = "chemicals";
    private const string Food = "food";

    private const string ToxinGroup = "Toxin";
    private const string BurnGroup = "Burn";
    private const string BruteGroup = "Brute";
    private const string Blunt = "Blunt";
    private const string Asphyxiation = "Asphyxiation";
    private const string Poison = "Poison";

    private const string SpaceCash2500 = "SpaceCash2500";
    private const string MobCatCake = "MobCatCake";
    private const string MobCorgiCerberus = "MobCorgiCerberus";
    private const string ArrivalBeaconTag = "station-beacon-arrivals";
    private const string BridgeBeaconTag = "station-beacon-bridge";
    private const string Omnizine = "Omnizine";
    private const string JusticeItems = "JusticeItems";
    private const string RandomArcadeSpawner = "RandomArcadeSpawner";
    private const string RandomMessageToChat = "RandomMessageToChat";
    private const string RandomAnomalyInjectorsSpawn = "RandomAnomalyInjectorsSpawn";
    private const string PinpointerProto = "PinpointerUplink";
    private const string StrangePill = "StrangePill";
    private const string ClusterBangFull = "ClusterBangFull";
}
