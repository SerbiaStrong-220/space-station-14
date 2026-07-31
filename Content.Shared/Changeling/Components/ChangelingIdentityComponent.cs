// SS220 Changeling
using Content.Shared.Cloning;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// The storage component for Changelings, it handles the link between a changeling and its consumed identities
/// that exist on a paused map.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ChangelingIdentityComponent : Component
{
    /// <summary>
    /// Prevents the initial identity capture from running when persistent identity state is installed on
    /// an already map-initialized body during a body transfer.
    /// </summary>
    [ViewVariables]
    public bool IdentityInitialized;

    /// <summary>
    /// Set briefly while ownership of the stored identities is moved to another body. This prevents the
    /// normal shutdown path from deleting the nullspaced identity entities during a body swap.
    /// </summary>
    [ViewVariables]
    public bool SuppressShutdownCleanup;

    /// <summary>
    /// Maximum number of identity samples that can be held at once. The identity currently worn by the
    /// changeling is not a sample and therefore is not counted after it has been consumed by a transform.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxStoredIdentities = 5;

    /// <summary>
    /// The list of entities that exist on a paused map. They are paused clones of the victims that the ling has consumed, with all relevant components copied from the original.
    /// The key is the EntityUid of the stored identity, the value is the original entity the identity came from.
    /// The value will be set to null if that entity is deleted.
    /// </summary>
    // TODO: This should be handled via a relation system in the future.
    [DataField]
    public Dictionary<EntityUid, EntityUid?> ConsumedIdentities = new();

    /// <summary>
    /// Owner-visible identity snapshots. Unlike <see cref="ConsumedIdentities"/>, this contains no references to
    /// the original bodies, which may be outside the owner's PVS.
    /// </summary>
    [AutoNetworkedField]
    public HashSet<EntityUid> StoredIdentities = [];

    /// <summary>
    /// Stable DNA identifier for every stored identity. This is kept separately from the original entity so
    /// that identities remain usable after a body is deleted and so duplicate DNA can be rejected.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, string> StoredGenomes = new();

    /// <summary>
    /// Unique genomes acquired by this changeling during the round. Unlike <see cref="StoredGenomes"/>, this
    /// is cumulative and is used by objectives. Transforming never reduces it.
    /// </summary>
    [DataField]
    public HashSet<string> AbsorbedGenomes = new();

    /// <summary>
    /// The currently assumed identity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentIdentity;

    /// <summary>
    /// DNA identifier of the form currently worn by the changeling. Used by form-sensitive objectives even
    /// after the corresponding stored sample has been consumed.
    /// </summary>
    [DataField]
    public string? CurrentGenome;

    /// <summary>
    /// The cloning settings passed to the CloningSystem, contains a list of all components to copy or have handled by their
    /// respective systems.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> IdentityCloningSettings = "ChangelingCloningSettings";

    public override bool SendOnlyToOwner => true;
}
