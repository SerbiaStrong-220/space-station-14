using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SpeciesWearRestriction;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class SpeciesWearRestrictionComponent : Component
{
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> AllowedSpecies = new();

    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> RestrictedSpecies = new();

    [DataField]
    public LocId FailedEquipPopup = string.Empty;
}
