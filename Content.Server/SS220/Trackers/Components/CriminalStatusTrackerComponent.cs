// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CriminalRecords;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server.SS220.Trackers.Components;

[RegisterComponent]
public sealed partial class CriminalStatusTrackerComponent : Component
{
    /// <summary>
    /// Mainly used to prevent starting sequence without anyone notice
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid TrackedByMind;

    [ViewVariables(VVAccess.ReadOnly)]
    public CriminalStatusTrackerSpecifier CriminalStatusSpecifier = new();

    [ViewVariables(VVAccess.ReadWrite)]
    private int _currentNode = InitCurrentNode;

    private const int InitCurrentNode = -1;

    public void ForceFirstNode() => _currentNode = InitCurrentNode;
    public void ForceLastNode() => _currentNode = CriminalStatusSpecifier.CriminalStatusNodes.Count;
    public float GetProgress() => (float)(_currentNode + 1) / CriminalStatusSpecifier.CriminalStatusNodes.Count;


    /// <summary>
    /// Hide logic for deciding if we can move further with new status. Moving to Last entry of the list is prioritized.
    /// </summary>
    /// <returns>True if node changed, else - false</returns>
    public bool TryMove(ProtoId<CriminalStatusPrototype> newStatus, EntityUid? mindUid)
    {
        if (CriminalStatusSpecifier.CriminalStatusNodes.Count == 0)
        {
            Log.Warning("Tried to move into empty list of criminal status nodes! This may occur to admins adding component");
            return false;
        }

        var newNode = _currentNode + 1; // nope no ++, -- playing
        if (TryChangeNode(newNode, newStatus, mindUid, ref _currentNode))
            return true;

        newNode = _currentNode - 1;
        if (TryChangeNode(newNode, newStatus, mindUid, ref _currentNode))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if current step is changeable by this mind
    /// </summary>
    /// <returns>True if current step allowed by CriminalStatusSpecifier</returns>
    public bool CanBeChangedByMind(EntityUid? mindUid)
    {
        return TrackedByMind != mindUid;
    }

    /// <returns> False if current step can be done by anyone, otherwise true. </returns>
    public bool NeedToCheckMind(int node)
    {
        return !CriminalStatusSpecifier.CriminalStatusNodes[node].CanBeSetByTracker;
    }

    private bool TryChangeNode(int newNode, ProtoId<CriminalStatusPrototype> newStatus, EntityUid? mindUid, ref int currentNode)
    {
        if (newNode <= InitCurrentNode)
            return false;

        if (newNode >= CriminalStatusSpecifier.CriminalStatusNodes.Count)
            return false;

        if (CriminalStatusSpecifier.CriminalStatusNodes[newNode].AllowedStatuses.Contains(newStatus)
            && (!NeedToCheckMind(newNode) || CanBeChangedByMind(mindUid)))
        {
            currentNode = newNode;
            return true;
        }

        return false;
    }
}

[DataDefinition]
public sealed partial class CriminalStatusTrackerSpecifier
{
    [DataField(required: true)]
    public List<CriminalStatusTrackerSpecifierNode> CriminalStatusNodes = new();
}

[DataDefinition]
public sealed partial class CriminalStatusTrackerSpecifierNode
{
    [DataField(required: true)]
    public List<ProtoId<CriminalStatusPrototype>> AllowedStatuses = new();

    [DataField]
    public bool CanBeSetByTracker;
}
