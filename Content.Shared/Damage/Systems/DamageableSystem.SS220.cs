using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    public bool SupportsGroup(ProtoId<DamageContainerPrototype>? container, ProtoId<DamageGroupPrototype> groupId)
    {
        if (container is null)
            return true;

        if (!_prototypeManager.TryIndex(groupId, out var group))
            return false;

        foreach (var damageType in group.DamageTypes)
        {
            if (SupportsType(container, damageType))
                return true;
        }

        return false;
    }
}

