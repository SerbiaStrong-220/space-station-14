// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph;

/// <summary>
/// All-wide class for requirement in surgery graph and edge
/// </summary>
public abstract partial class SurgeryGraphRequirement
{
    [DataField(tag: "subject", required: true)]
    private SurgeryGraphRequirementSubject _subject = SurgeryGraphRequirementSubject.Target;

    // just paranoid about someone writing in it
    public SurgeryGraphRequirementSubject Subject => _subject;

    [DataField(required: true)]
    public LocId Description;

    [DataField(required: true)]
    public LocId FailureMessage;

    public abstract bool SatisfiesRequirements(EntityUid? uid, IEntityManager entityManager);

    [MustCallBase(true)]
    public virtual bool MeetRequirement(EntityUid? uid, IEntityManager entityManager)
    {
        return SatisfiesRequirements(uid, entityManager);
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
    Container
}
