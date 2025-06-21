using Content.Server.Forensics;
using Content.Shared.SS220.Trap;

namespace Content.Server.SS220.ServerTrap;

/// <summary>
/// <see cref="TrapSystem"/>
/// </summary>
public sealed class ServerTrapSystem : EntitySystem
{
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TrapComponent, TrapAfterTriggerEvent>(OnAfterTrigger);
    }

    private void OnAfterTrigger(Entity<TrapComponent> ent, ref TrapAfterTriggerEvent args)
    {
        if (!args.Activator.HasValue)
            return;

        _forensicsSystem.TransferDna(args.Item,args.Activator.Value);
    }
}
