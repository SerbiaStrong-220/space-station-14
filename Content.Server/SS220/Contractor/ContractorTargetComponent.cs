using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Server.SS220.Contractor;

/// <summary>
/// This is used for marking target for contractor.
/// </summary>
[RegisterComponent]
public sealed partial class ContractorTargetComponent : Component
{
    /// <summary>
    ///     Contractor entity
    /// </summary>
    [DataField]
    public EntityUid Performer;

    /// <summary>
    ///     The amount of TC, given to the contractor after a successful contract.
    /// </summary>
    [DataField]
    public FixedPoint2 AmountTc;

    /// <summary>
    ///     The coordinates, where the target entered the contractor portal
    /// </summary>
    [DataField]
    public EntityCoordinates PortalPosition;

    /// <summary>
    ///     The time, in what moment target was entered in portal
    /// </summary>
    [DataField]
    public TimeSpan EnteredPortalTime;

    /// <summary>
    ///     The portal entity, which contractor opened on station
    /// </summary>
    [DataField]
    public EntityUid? PortalOnStationEntity;

    /// <summary>
    ///     The portal entity, which contractor opened on tajpan
    /// </summary>
    [DataField]
    public EntityUid? PortalOnTajpanEntity;

    /// <summary>
    ///     The boolean, which indicates, if the target can be assigned to another contractor
    /// </summary>
    public bool CanBeAssigned = true;
}
