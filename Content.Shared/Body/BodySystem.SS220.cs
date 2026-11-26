// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Forensics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Body;

public sealed partial class BodySystem
{
    /// <summary>
    /// A part that left the body in any way (surgery, dismemberment, gibbing) carries the owner's DNA, like blood on it would.
    /// </summary>
    private void LeaveDnaOnOrgan(EntityUid body, EntityUid organ)
    {
        if (TerminatingOrDeleted(organ) || TerminatingOrDeleted(body))
            return;

        var ev = new TransferDnaEvent { Donor = body, Recipient = organ, CanDnaBeCleaned = false };
        RaiseLocalEvent(body, ref ev);
    }

    public bool TryGetOrganByCategory(
        Entity<BodyComponent?> ent,
        ProtoId<OrganCategoryPrototype> category,
        [NotNullWhen(true)] out Entity<OrganComponent>? foundOrgan)
    {
        foundOrgan = null;
        if (!_bodyQuery.Resolve(ent, ref ent.Comp, logMissing: false) || ent.Comp.Organs is null)
            return false;

        foreach (var organUid in ent.Comp.Organs.ContainedEntities)
        {
            if (!_organQuery.TryComp(organUid, out var organComp))
                continue;

            if (organComp.Category != category)
                continue;

            foundOrgan = (organUid, organComp);
            return true;
        }

        return false;
    }

    public bool HasOrganCategory(Entity<BodyComponent?> ent, ProtoId<OrganCategoryPrototype> category)
    {
        return TryGetOrganByCategory(ent, category, out _);
    }

    public bool IsOrganOfCategory(Entity<OrganComponent?> organ, ProtoId<OrganCategoryPrototype> category)
    {
        if (!_organQuery.Resolve(organ, ref organ.Comp, logMissing: false))
            return false;

        return organ.Comp.Category == category;
    }

    public bool TryExtractOrgan(
        Entity<BodyComponent?> ent,
        ProtoId<OrganCategoryPrototype> category,
        [NotNullWhen(true)] out Entity<OrganComponent>? extractedOrgan)
    {
        extractedOrgan = null;
        if (!_bodyQuery.Resolve(ent, ref ent.Comp, logMissing: false) || ent.Comp.Organs is null)
            return false;

        if (!TryGetOrganByCategory(ent, category, out var organ))
            return false;

        if (!_container.Remove(organ.Value.Owner, ent.Comp.Organs))
            return false;

        DebugTools.Assert(!ent.Comp.Organs.Contains(organ.Value.Owner), $"Extracted organ {ToPrettyString(organ.Value)} is still in the body");
        extractedOrgan = organ;
        return true;
    }

    public bool TryInsertOrgan(Entity<BodyComponent?> ent, Entity<OrganComponent?> organ)
    {
        if (!_bodyQuery.Resolve(ent, ref ent.Comp, logMissing: false) || ent.Comp.Organs is null)
            return false;

        if (!_organQuery.Resolve(organ, ref organ.Comp, logMissing: false))
            return false;

        if (organ.Comp.Category is { } category && HasOrganCategory(ent, category))
            return false;

        return _container.Insert(organ.Owner, ent.Comp.Organs);
    }
}
