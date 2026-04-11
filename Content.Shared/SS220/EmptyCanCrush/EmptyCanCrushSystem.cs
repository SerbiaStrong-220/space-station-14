using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.SS220.EmptyCanCrush;

public sealed class EmptyCanCrushSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmptyCanCrushComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

    }

    private void OnGetVerbs(Entity<EmptyCanCrushComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        args.Verbs.Add(new()
        {
            Act = () => TryCrush(entity),
            Text = Loc.GetString("can-crush-verb-text"),
            Message = Loc.GetString("can-crush-verb-message"),
        });
    }
    private void TryCrush (Entity<EmptyCanCrushComponent> entity)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(entity, out var transform))
            return;
        PredictedSpawnAtPosition(entity.Comp.CrushedCanId, transform.Coordinates);
        _audio.PlayPredicted(entity.Comp.CrushSound, entity, entity);
        PredictedDel(entity);
    }
}
