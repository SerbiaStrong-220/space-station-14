using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Abilities;

[Serializable, NetSerializable]
public enum NarsiAltarAbilitiesInterfaceKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class NarsiAbilitiesState : BoundUserInterfaceState
{
    public List<NarsiAbilityUIModel> OpenedAbilities;
    public List<NarsiAbilityUIModel> ClosedAbilities;
    public int BloodScore;
    public bool IsLeader;

    public NarsiAbilitiesState(List<NarsiAbilityUIModel> openedAbilities, List<NarsiAbilityUIModel> closedAbilities, int bloodScore, bool isLeader)
    {
        OpenedAbilities = openedAbilities;
        ClosedAbilities = closedAbilities;
        BloodScore = bloodScore;
        IsLeader = isLeader;
    }
}

[Serializable, NetSerializable]
public sealed class NarsiAbilityOpenMessage : BoundUserInterfaceMessage
{
    public string AbilityId;

    public NarsiAbilityOpenMessage(string abilityId)
    {
        AbilityId = abilityId;
    }
}

[Serializable, NetSerializable]
public sealed class NarsiAbilityLearnMessage : BoundUserInterfaceMessage
{
    public string AbilityId;

    public NarsiAbilityLearnMessage(string abilityId)
    {
        AbilityId = abilityId;
    }
}
