// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MartialArts;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMartialArtsSystem))]
public sealed partial class MartialArtistComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<MartialArtPrototype>? MartialArt;

    [DataField]
    public List<CombatSequenceStep> CurrentSteps = new();

    [DataField]
    public TimeSpan LastStepPerformedAt = TimeSpan.Zero;

    [DataField]
    public TimeSpan SequenceTimeout = TimeSpan.FromSeconds(2);

    [DataField]
    public float UpdateRate = 1; // per second

    [DataField]
    public float UpdateAccumulator = 0;
}

[Serializable, NetSerializable]
public sealed partial class MartialArtistComponentState(List<CombatSequenceStep> steps, TimeSpan lastStepPerformedAt) : ComponentState
{
    public List<CombatSequenceStep> CurrentSteps = steps;
    public TimeSpan LastStepPerformedAt = lastStepPerformedAt;
}
