// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class EmptyInventorySlotRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public HashSet<string> SlotNames;

    protected override bool Requirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (uid is null)
            return true;

        if (!uid.Value.Valid)
            return false;

        foreach (var slotName in SlotNames)
        {
            if (!entityManager.System<InventorySystem>().TryGetSlotContainer(uid.Value, slotName, out var containerSlot, out _))
                continue;

            if (containerSlot.ContainedEntity is null)
                continue;

            return false;
        }

        return true;
    }

    public override string RequirementFailureReason(EntityUid? uid, IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        if (uid is null)
            return string.Empty;

        return Loc.GetString(FailureMessage, ("target", Identity.Entity(uid.Value, entityManager)));
    }
}
