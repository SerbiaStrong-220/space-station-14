// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

/// <summary>
/// Adds components to the patient, the ones already there are kept.
/// </summary>
[DataDefinition]
public sealed partial class AddComponentsAction : ISurgeryGraphEdgeAction
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        entityManager.AddComponents(targetUid, Components, removeExisting: false);
    }
}
