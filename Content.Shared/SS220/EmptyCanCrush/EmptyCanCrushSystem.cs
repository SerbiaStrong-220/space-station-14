//© SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.SS220.EmptyCanCrush;

public sealed class EmptyCanCrushSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

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

        var user = args.User;
        args.Verbs.Add(new()
        {
            Act = () => TryCrush(entity, user),
            Text = Loc.GetString("can-crush-verb-text"),
            Message = Loc.GetString("can-crush-verb-message"),
        });
    }

    private void TryCrush(Entity<EmptyCanCrushComponent> entity, EntityUid user)
    {
        if (!TryComp(entity, out TransformComponent? transform))
            return;

        if (!_tagSystem.HasAllTags(entity, TrashTag, CanTag))
        {
            _popup.PopupPredicted(Loc.GetString("try-crush-can-false"), entity.Owner, null);
            return;
        }

        var crushedCan = PredictedSpawnAtPosition(entity.Comp.CrushedCanId, transform.Coordinates);

        if (!Exists(crushedCan))
            return;

        _audio.PlayPredicted(entity.Comp.CrushSound, crushedCan, entity);

        if (_handsSystem.IsHolding(user, entity, out var handID))
            _handsSystem.TryForcePickup(user, crushedCan, handID);

        PredictedQueueDel(entity.Owner);
    }
}
