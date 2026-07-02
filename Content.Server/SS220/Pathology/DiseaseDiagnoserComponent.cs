// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pathology;

[RegisterComponent]
public sealed partial class DiseaseDiagnoserComponent : Component
{
    /// <summary>
    /// Item slot holding the sample container.
    /// </summary>
    [DataField]
    public string SlotId = "diagnoserSlot";

    /// <summary>
    /// How long a scan takes.
    /// </summary>
    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Internal buffer solution that holds stable mutagen for copying.
    /// </summary>
    [DataField]
    public string BufferSolutionId = "buffer";

    /// <summary>
    /// Reagent used to copy a virus.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> MutagenReagent = "StableMutagen";

    /// <summary>
    /// Unstable mutagen reagent — also lights the mutagen vial sprite, but isn't usable for copying.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> UnstableMutagenReagent = "UnstableMutagen";

    /// <summary>
    /// Amount of reagent one copy costs, also amount ends up in the produced bottle.
    /// </summary>
    [DataField]
    public FixedPoint2 CopyAmount = 15;

    /// <summary>
    /// Container spawned to hold a copied virus.
    /// </summary>
    [DataField]
    public EntProtoId CopyBottle = "ChemistryEmptyVial";

    /// <summary>
    /// Solution in produced bottle.
    /// </summary>
    [DataField]
    public string BottleSolutionId = "drink";

    /// <summary>How long print takes, keep in sync with animation length.</summary>
    [DataField]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(3);

    /// <summary>Paper form.</summary>
    [DataField]
    public string FormCollection = "nanotrasen_station";

    [DataField]
    public string FormGroup = "medical";

    [DataField]
    public string FormId = "med_rep_virus";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    /// <summary>
    /// When current scan finishes.
    /// </summary>
    public TimeSpan? ScanEndTime;

    /// <summary>
    /// When current form print finishes.
    /// </summary>
    public TimeSpan? PrintEndTime;

    public bool HasResult;

    /// <summary>One block per virus.</summary>
    public List<DiseaseDiagnoserVirusResult> ResultViruses = new();
}
