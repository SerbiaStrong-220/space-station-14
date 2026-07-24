// SS220 Changeling
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Systems;

/// <summary>
/// Send when a player selects an identity to transform into in the radial menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the stored identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}

/// <summary>
/// Send when a player selects an identity to drop from their storage.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentityDropMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The uid of the stored identity.
    /// </summary>
    public readonly NetEntity TargetIdentity = targetIdentity;
}

// SS220 changeling transformation sting begin
/// <summary>
/// Sent when a changeling selects the identity that should be imposed on a previously selected sting target.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformationStingIdentitySelectMessage(NetEntity targetIdentity) : BoundUserInterfaceMessage
{
    public readonly NetEntity TargetIdentity = targetIdentity;
}
// SS220 changeling transformation sting end

[Serializable, NetSerializable]
public enum ChangelingTransformUiKey : byte
{
    Key,
    // SS220 changeling transformation sting
    TransformationSting,
}
