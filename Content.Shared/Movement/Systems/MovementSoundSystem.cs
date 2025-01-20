using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Plays a sound on MoveInputEvent.
/// </summary>
public sealed class MovementSoundSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // SS220 performance-test-begin
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    private bool _lessSound;
    // SS220 performance-test-end

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MovementSoundComponent, MoveInputEvent>(OnMoveInput);

        Subs.CVar(_configManager, CCVars220.LessSoundSources, value => _lessSound = value, true); // SS220 performance-test
    }

    private void OnMoveInput(Entity<MovementSoundComponent> ent, ref MoveInputEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var oldMoving = (SharedMoverController.GetNormalizedMovement(args.OldMovement) & MoveButtons.AnyDirection) != MoveButtons.None;
        var moving = (SharedMoverController.GetNormalizedMovement(args.Entity.Comp.HeldMoveButtons) & MoveButtons.AnyDirection) != MoveButtons.None;

        if (oldMoving == moving)
            return;

        // SS220 performance-test
        if (_lessSound)
            return;
        // SS220 performace-test

        if (moving)
        {
            DebugTools.Assert(ent.Comp.SoundEntity == null);
            ent.Comp.SoundEntity = _audio.PlayPredicted(ent.Comp.Sound, ent.Owner, ent.Owner)?.Entity;
        }
        else
        {
            ent.Comp.SoundEntity = _audio.Stop(ent.Comp.SoundEntity);
        }
    }
}
