// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Virology;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Virology;

[RegisterComponent]
public sealed partial class VirusDiagnoserComponent : Component
{
    /// <summary>Slot holding vial.</summary>
    [DataField]
    public string SlotId = "diagnoserSlot";

    /// <summary>How long a scan takes.</summary>
    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(4);

    /// <summary>Internal buffer that holds reagent for copying.</summary>
    [DataField]
    public string BufferSolutionId = "buffer";

    /// <summary>Reagent used to copy a virus.</summary>
    [DataField]
    public ProtoId<ReagentPrototype> MutagenReagent = "StableMutagen";

    [DataField]
    public ProtoId<ReagentPrototype> UnstableMutagenReagent = "UnstableMutagen";

    /// <summary>Amount of reagent copy cost, also amount in produced vial.</summary>
    [DataField]
    public FixedPoint2 CopyAmount = 15;

    /// <summary>Container spawned to hold a copied virus.</summary>
    [DataField]
    public EntProtoId CopyBottle = "ChemistryEmptyVial";

    /// <summary>Solution in produced bottle.</summary>
    [DataField]
    public string BottleSolutionId = "drink";

    /// <summary>How long paper print takes (kept in sync with the animation length).</summary>
    [DataField]
    public TimeSpan PrintDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public string FormCollection = "nanotrasen_station";

    [DataField]
    public string FormGroup = "medical";

    [DataField]
    public string FormId = "med_rep_virus";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    public TimeSpan? ScanEndTime;

    public TimeSpan? PrintEndTime;

    public bool HasResult;

    /// <summary>One block per virus.</summary>
    public List<VirusDiagnoserResult> ResultViruses = [];
}
