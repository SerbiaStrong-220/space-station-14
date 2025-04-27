using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// This handles...
/// </summary>
public sealed class PreparationTrapSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PreparationTrapComponent, UseInHandEvent>(OnAfterInteract);
        SubscribeLocalEvent<PreparationTrapComponent, SetTrapEvent>(OnAfterSetTrap);
    }

    private void OnAfterInteract(Entity<PreparationTrapComponent> ent, ref UseInHandEvent args)
    {
        // if(!TryComp<CultYoggComponent>(args.User, out var cultYoggComp))
        //     return;

        // if(cultYoggComp.CurrentStage == CultYoggStage.Alarm)
        // {
        //     _popup.PopupEntity(Loc.GetString("trap-component-max-value"), args.User, args.User);
        //     return;
        // }

        // if (TrapSystem.TrapList.Count > ent.Comp.MaxTrap || ent.Comp.MaxTrap != 0)
        // {
        //     _popup.PopupEntity(Loc.GetString("trap-component-max-value"), args.User, args.User);
        //     return;
        // }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.SetTrapDelay,
            new SetTrapEvent(),
            ent.Owner,
            target: args.User,
            used: ent.Owner)
        {
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnAfterSetTrap(Entity<PreparationTrapComponent> ent, ref SetTrapEvent args)
    {
        if(args.Cancelled)
            return;

        Spawn(ent.Comp.TrapProtoType, _transform.GetMoverCoordinates(args.User));
    }
}
