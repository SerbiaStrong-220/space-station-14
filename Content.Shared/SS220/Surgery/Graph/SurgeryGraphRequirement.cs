// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph;

/// <summary>
/// All-wide class for requirement in surgery graph and edge
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class SurgeryGraphRequirement
{
    [DataField(tag: "subject", required: true)]
    private SurgeryGraphRequirementSubject _subject = SurgeryGraphRequirementSubject.Target;

    // just paranoid about someone writing in it
    public SurgeryGraphRequirementSubject Subject => _subject;

    [DataField]
    protected bool Invert = false;

    [DataField(required: true)]
    protected LocId Description;

    [DataField(required: true)]
    protected LocId FailureMessage;

    /// <summary>
    /// Called to check if requirement met
    /// </summary>
    protected abstract bool Requirement(EntityUid? uid, IEntityManager entityManager);

    /// <summary>
    /// Called when we want to met requirement and it satisfies to make something after like consuming reagents
    /// </summary>
    protected virtual void AfterRequirementMet(EntityUid? uid, IEntityManager entityManager) { }

    public bool SatisfiesRequirements(EntityUid? uid, IEntityManager entityManager)
    {
        if (uid.HasValue && !uid.Value.IsValid())
            return false;

        return Invert != Requirement(uid, entityManager);
    }

    public bool MeetRequirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (!SatisfiesRequirements(uid, entityManager))
            return false;

        AfterRequirementMet(uid, entityManager);
        return true;
    }

    public virtual string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString(Description);
    }

    public virtual string RequirementFailureReason(EntityUid? uid, IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString(FailureMessage);
    }
}

public enum SurgeryGraphRequirementSubject : int
{
    Target,
    Performer,
    Used,
    Strap
}
