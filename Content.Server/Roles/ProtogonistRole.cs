using Content.Shared.Roles;

namespace Content.Server.Roles;

public abstract class ProtogonistRole : Role
{
    public ProtogonistPrototype Prototype { get; }

    public override string Name { get; }

    public override bool Antagonist { get; } = default!;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="mind">A mind (player)</param>
    /// <param name="protogPrototype">Protogonist prototype</param>
    protected ProtogonistRole(Mind.Mind mind, ProtogonistPrototype protogPrototype) : base(mind)
    {
        Prototype = protogPrototype;
        Name = Loc.GetString(protogPrototype.Name);
        Antagonist = protogPrototype.Antagonist;
    }
}
