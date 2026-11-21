// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Body;

/// <summary>
/// Limb organ that carries the organs attached to it: they leave the body inside the limb and come back with it,
/// e.g. a severed arm takes its hand along.
/// </summary>
[RegisterComponent, Access(typeof(LimbSystem))]
public sealed partial class LimbComponent : Component
{
    public const string ContainerId = "limb_attached_organs";

    [DataField(required: true)]
    public List<ProtoId<OrganCategoryPrototype>> AttachedOrgans = new();

    /// <summary>
    /// A body without any supporting limb can't stand and only crawls.
    /// </summary>
    [DataField]
    public bool Supporting;

    [ViewVariables]
    public Container Container = default!;
}
