using Content.Server.Chat.Managers;
using Content.Shared.PDA;
using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class KontrrazvedchikRole : ProtogonistRole
{
    public KontrrazvedchikRole(Mind.Mind mind, ProtogonistPrototype protoPrototype) : base(mind, protoPrototype) { }
}

