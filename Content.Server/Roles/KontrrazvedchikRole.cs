using Content.Server.Chat.Managers;
using Content.Shared.PDA;
using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class KontrrazvedchikRole : AntagonistRole
{
    public KontrrazvedchikRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}

