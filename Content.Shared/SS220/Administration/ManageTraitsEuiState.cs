using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class ManageTraitsEuiState : EuiStateBase
{
    public string TargetName { get; }
    public List<string> ActiveTraitIds { get; }
    public Dictionary<string, string> AllTraits { get; }

    public ManageTraitsEuiState(string targetName, List<string> activeTraitIds, Dictionary<string, string> allTraits)
    {
        TargetName = targetName;
        ActiveTraitIds = activeTraitIds;
        AllTraits = allTraits;
    }
}

[Serializable, NetSerializable]
public sealed class AddTraitMessage : EuiMessageBase
{
    public string TraitId { get; }
    public bool SpawnGear { get; }

    public AddTraitMessage(string traitId, bool spawnGear)
    {
        TraitId = traitId;
        SpawnGear = spawnGear;
    }
}

[Serializable, NetSerializable]
public sealed class RemoveTraitMessage : EuiMessageBase
{
    public string TraitId { get; }
    public RemoveTraitMessage(string traitId) { TraitId = traitId; }
}
