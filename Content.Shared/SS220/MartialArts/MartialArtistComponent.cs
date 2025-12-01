// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MartialArts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(MartialArtsSystem))]
public sealed partial class MartialArtistComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public ProtoId<MartialArtPrototype>? MartialArt;

    [DataField]
    [AutoNetworkedField]
    public List<CombatSequenceStep> CurrentSteps = [];

    [DataField]
    [AutoNetworkedField]
    public TimeSpan LastStepPerformedAt = TimeSpan.Zero;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan SequenceTimeout = TimeSpan.FromSeconds(2);

    [DataField]
    [AutoNetworkedField]
    public TimeSpan LastSequencePerformedAt = TimeSpan.Zero;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan LastSequenceCooldown = TimeSpan.Zero;
}

[Serializable, NetSerializable]
public sealed partial class MartialArtistComponentState(List<CombatSequenceStep> steps, TimeSpan lastStepPerformedAt) : ComponentState
{
    public List<CombatSequenceStep> CurrentSteps = steps;
    public TimeSpan LastStepPerformedAt = lastStepPerformedAt;
}
