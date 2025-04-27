using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.Trap;

namespace Content.Shared.SS220.CultYogg.FruitTrap;

/// <summary>
/// This handles...
/// </summary>
public sealed class CultYoggItemFruitTrapSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CultYoggItemFruitTrapComponent, DoAfterAttemptEvent<SetTrapEvent>>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<CultYoggItemFruitTrapComponent> ent, ref DoAfterAttemptEvent<SetTrapEvent> args)
    {

    }
}
