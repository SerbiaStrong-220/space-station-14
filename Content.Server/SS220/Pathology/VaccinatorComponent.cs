// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pathology;

[RegisterComponent]
public sealed partial class VaccinatorComponent : Component
{
    [DataField]
    public string SlotId = "vaccinatorSlot";

    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public string BufferSolutionId = "buffer";

    /// <summary>Reagent consumed from the buffer to create vaccine.</summary>
    [DataField]
    public ProtoId<ReagentPrototype> TricordrazineReagent = "Tricordrazine";

    /// <summary>Reagent the produced vaccine is made of (carries the immunity data).</summary>
    [DataField]
    public ProtoId<ReagentPrototype> VaccineReagent = "Vaccine";

    /// <summary>How much tricordrazine one vaccine costs, and how much vaccine ends up in the bottle.</summary>
    [DataField]
    public FixedPoint2 VaccineAmount = 15;

    [DataField]
    public EntProtoId VaccineBottle = "ChemistryEmptyVial";

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
    public string FormId = "med_rep_vaccine";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    public TimeSpan? ScanEndTime;
    public TimeSpan? PrintEndTime;

    public bool HasResult;

    /// <summary>One block per virus.</summary>
    public List<VaccinatorVirusResult> ResultViruses = new();
}
