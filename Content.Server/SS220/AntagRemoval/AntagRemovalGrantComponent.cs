// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.AntagRemoval;

/// <summary>Transient provenance for grants by AntagSelectionSystem; not serialized.</summary>
[RegisterComponent]
public sealed partial class AntagRemovalGrantComponent : Component
{
    public readonly Dictionary<string, AntagGrantRecord> Grants = new();
    public HashSet<ProtoId<NpcFactionPrototype>>? FactionsBeforeFirstAntag;
    public HashSet<ProtoId<NpcFactionPrototype>>? FactionsBeforeCultConversion;
    public bool HadPacifiedBeforeCultConversion;

}

public sealed class AntagGrantRecord
{
    public bool VerifiedProvenance;
    // Exact Mind that received the role. MindRoles do not belong to bodies.
    public EntityUid? GrantedMindId;
    public EntityUid GameRule;
    // Exact definition index at assignment time. -1 for midround conversions.
    public int SelectionDefinitionIndex = -1;
    public string RoleId = string.Empty;
    public readonly HashSet<EntityUid> Objectives = new();
    public readonly HashSet<EntityUid> Gear = new();

    public readonly HashSet<ProtoId<NpcFactionPrototype>> FactionsAdded = new();
    public readonly HashSet<ProtoId<NpcFactionPrototype>> FactionsRemoved = new();
    // Exact entities created for this traitor, not every uplink owned by the Mind.
    public readonly HashSet<EntityUid> UplinkStores = new();
    public readonly HashSet<EntityUid> UplinkImplants = new();

    /// <summary>Only true for body components newly added by this role's selection.</summary>
    public bool GrantedThieving;
    public bool GrantedPacified;
    public bool GrantedPatientZero;
    public bool GrantedPendingZombie;
    public bool GrantedZombifyOnDeath;
    public bool GrantedIncurableZombie;
}
