// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AntagRemoval;

[Serializable, NetSerializable]
public sealed class RequestRemovableRolesEvent(Guid requestId, NetUserId target) : EntityEventArgs
{
    public readonly Guid RequestId = requestId;
    public readonly NetUserId Target = target;
}

[Serializable, NetSerializable]
public sealed class RemoveAntagRoleEvent(Guid requestId, NetUserId target, NetEntity body, NetEntity mind, string kind) : EntityEventArgs
{
    public readonly Guid RequestId = requestId;
    public readonly NetUserId Target = target;
    public readonly NetEntity Body = body;
    public readonly NetEntity Mind = mind;
    public readonly string Kind = kind;
}

[Serializable, NetSerializable]
public sealed class RemovableRolesEvent(
    Guid requestId,
    NetUserId target,
    NetEntity? body,
    NetEntity? mind,
    Dictionary<string, string> roles,
    string message,
    List<string>? unsupportedRoles = null) : EntityEventArgs
{
    public readonly Guid RequestId = requestId;
    public readonly NetUserId Target = target;
    public readonly NetEntity? Body = body;
    public readonly NetEntity? Mind = mind;
    public readonly Dictionary<string, string> Roles = roles;
    public readonly string Message = message;
    public readonly List<string> UnsupportedRoles = unsupportedRoles ?? new();
}
