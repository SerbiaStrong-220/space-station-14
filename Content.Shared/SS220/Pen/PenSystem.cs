using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.SS220.Signature;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pen;

public sealed class PenSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PenComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<PaperComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnGetVerbs(Entity<PenComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        args.Verbs.UnionWith(CreateVerb(ent, args.User));
    }

    private void OnUIOpened(Entity<PaperComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent, args.Actor);
    }

    private List<Verb> CreateVerb(Entity<PenComponent> ent, EntityUid user)
    {
        List<Verb> verbs = [];

        foreach (var writeSize in Enum.GetValues<PenWriteSize>())
        {
            var writeVerb = new Verb
            {
                Text = Loc.GetString("pen-brush-write-" + writeSize),
                Disabled = ent.Comp.BrushWriteSize == (int)writeSize,
                Act = () =>
                {
                    ent.Comp.BrushWriteSize = (int)writeSize;
                    Dirty(ent);

                    UpdateUI(ent, user);
                },
                Priority = (int)writeSize,
                Category = VerbCategory.PenWriteSize,
            };

            verbs.Add(writeVerb);
        }

        foreach (var eraseSize in Enum.GetValues<PenEraseSize>())
        {
            var eraseVerb = new Verb
            {
                Text = Loc.GetString("pen-brush-erase-" + eraseSize),
                Disabled = ent.Comp.BrushEraseSize == (int)eraseSize,
                Act = () =>
                {
                    ent.Comp.BrushEraseSize = (int)eraseSize;
                    Dirty(ent);

                    UpdateUI(ent, user);
                },
                Priority = (int)eraseSize,
                Category = VerbCategory.PenEraseSize,
            };

            verbs.Add(eraseVerb);
        }

        return verbs;
    }

    private void UpdateUI(Entity<PenComponent> ent, EntityUid user)
    {
        var enumerable = _ui.GetActorUis(user);

        foreach (var ui in enumerable)
        {
            if (ui.Key is not PaperComponent.PaperUiKey.Key)
                continue;

            var state = new UpdatePenBrushPaperState(ent.Comp.BrushWriteSize, ent.Comp.BrushEraseSize);
            _ui.SetUiState(ui.Entity, ui.Key, state);
        }
    }

    private void UpdateUI(Entity<PaperComponent> ent, EntityUid user)
    {
        Entity<PenComponent>? pen = null;

        var enumerate = _hands.EnumerateHeld(user);

        foreach (var handItem in enumerate)
        {
            if (!TryComp<PenComponent>(handItem, out var penComp))
                continue;

            pen = (handItem, penComp);
            break;
        }

        if (pen == null)
            return;

        var enumerable = _ui.GetActorUis(user);

        foreach (var ui in enumerable)
        {
            if (ui.Key is not PaperComponent.PaperUiKey.Key)
                continue;

            var state = new UpdatePenBrushPaperState(pen.Value.Comp.BrushWriteSize, pen.Value.Comp.BrushEraseSize);
            _ui.SetUiState(ent.Owner, ui.Key, state);
        }
    }
}

[Serializable, NetSerializable]
public enum PenWriteSize
{
    Normal = 1,
    Medium = 2,
    Large = 4,
}

[Serializable, NetSerializable]
public enum PenEraseSize
{
    Normal = 2,
    Medium = 4,
    Large = 6,
}
