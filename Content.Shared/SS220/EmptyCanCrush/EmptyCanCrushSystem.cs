using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
namespace Content.Shared.SS220.EmptyCanCrush;

public sealed class EmptyCanCrushSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";
    private static readonly ProtoId<TagPrototype> CanTag = "DrinkCan";
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
            Text = Loc.GetString(Loc.GetString("can-crush-verb-text")),
            Message = Loc.GetString(Loc.GetString("can-crush-verb-message")),
        });
    }
    private void TryCrush (Entity<EmptyCanCrushComponent> entity)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(entity, out var transform))
            return;
        if (!(_tagSystem.HasTag(entity, TrashTag) && _tagSystem.HasTag(entity, CanTag)))
        {
            _popup.PopupPredicted(Loc.GetString("try-crush-can-false"), entity.Owner, null);
            return;
        }
        var crushedCan = PredictedSpawnAtPosition(entity.Comp.CrushedCanId, transform.Coordinates);
        if (crushedCan == null)
            return;
        _audio.PlayPredicted(entity.Comp.CrushSound, crushedCan, entity);
        PredictedQueueDel(entity.Owner);

    }

}
