// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.SS220.Signature;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.SS220.Pen;

public abstract class SharedPenSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public readonly Dictionary<int, LocId> PenBrushWriteNames = new()
    {
        [1] = "pen-brush-write-normal",
        [2] = "pen-brush-write-medium",
        [4] = "pen-brush-write-large",
    };

    public readonly Dictionary<int, LocId> PenBrushEraseNames = new()
    {
        [2] = "pen-brush-erase-normal",
        [4] = "pen-brush-erase-medium",
        [6] = "pen-brush-erase-large",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<PenComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<PaperComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnGetVerbs(Entity<PenComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract)
            return;

        args.Verbs.UnionWith(CreateVerbsForPainting(ent, args.User));
        args.Verbs.UnionWith(CreateVerbsForCopying(ent, args.User));
    }

    private void OnUIOpened(Entity<PaperComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUI(ent, args.Actor);
    }

    private List<Verb> CreateVerbsForPainting(Entity<PenComponent> ent, EntityUid user)
    {
        List<Verb> verbs = [];

        foreach (var writeSize in PenBrushWriteNames)
        {
            var writeVerb = new Verb
            {
                Text = Loc.GetString(writeSize.Value),
                Disabled = ent.Comp.BrushWriteSize == writeSize.Key,
                Act = () =>
                {
                    ent.Comp.BrushWriteSize = writeSize.Key;
                    Dirty(ent);

                    UpdateUI(ent, user);
                },
                Priority = -writeSize.Key,
                Category = VerbCategory.PenWriteSize,
            };

            verbs.Add(writeVerb);
        }

        foreach (var eraseSize in PenBrushEraseNames)
        {
            var eraseVerb = new Verb
            {
                Text = Loc.GetString(eraseSize.Value),
                Disabled = ent.Comp.BrushEraseSize == eraseSize.Key,
                Act = () =>
                {
                    ent.Comp.BrushEraseSize = eraseSize.Key;
                    Dirty(ent);

                    UpdateUI(ent, user);
                },
                Priority = -eraseSize.Key,
                Category = VerbCategory.PenEraseSize,
            };

            verbs.Add(eraseVerb);
        }

        return verbs;
    }

    private List<Verb> CreateVerbsForCopying(Entity<PenComponent> ent, EntityUid user)
    {
        List<Verb> verbs = [];

        if (!ent.Comp.CanCopySignature || !TryGetFirstOpenedPaper(user, out var paper))
            return verbs;

        var haveSignature = TryComp<SignatureComponent>(paper, out var signature) && signature.Data != null;

        var copySignatureVerb = new Verb
        {
            Text = Loc.GetString("verb-pen-signature-copy"),
            Disabled = !haveSignature,
            Act = () =>
            {
                if (signature == null)
                    return;

                ent.Comp.CopiedSignature = signature.Data;
                Dirty(paper.Value, signature);
            },
            Category = VerbCategory.PenCopying,
        };

        var pasteSignatureVerb = new Verb
        {
            Text = Loc.GetString("verb-pen-signature-paste"),
            Disabled = ent.Comp.CopiedSignature == null,
            Act = () =>
            {
                PasteSignatureAct(paper.Value, ent, user);
            },
            Category = VerbCategory.PenCopying,
        };

        verbs.Add(copySignatureVerb);
        verbs.Add(pasteSignatureVerb);

        return verbs;
    }

    private void UpdateUI(Entity<PenComponent> ent, EntityUid user, bool updateSignature = false)
    {
        var enumerable = _ui.GetActorUis(user);

        foreach (var ui in enumerable)
        {
            if (ui.Key is not PaperComponent.PaperUiKey.Key)
                continue;

            var state = new UpdatePenBrushPaperState(ent.Comp.BrushWriteSize, ent.Comp.BrushEraseSize);
            _ui.SetUiState(ui.Entity, ui.Key, state);

            if (!updateSignature || ent.Comp.CopiedSignature == null)
                continue;

            var fullState = new UpdateSignatureDataState(ent.Comp.CopiedSignature);
            _ui.SetUiState(ui.Entity, ui.Key, fullState);
        }
    }

    private void UpdateUI(Entity<PaperComponent> ent, EntityUid user, bool updateSignature = false)
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

        UpdateUI(pen.Value, user, updateSignature);
    }

    private bool TryGetFirstOpenedPaper(EntityUid user, [NotNullWhen(true)] out Entity<PaperComponent>? paper)
    {
        paper = null;
        var enumerable = _ui.GetActorUis(user);

        foreach (var ui in enumerable)
        {
            if (!TryComp<PaperComponent>(ui.Entity, out var paperComp))
                continue;

            paper = (ui.Entity, paperComp);
            return true;
        }

        return false;
    }

    protected virtual void PasteSignatureAct(Entity<PaperComponent> paper, Entity<PenComponent> pen, EntityUid user)
    {
        var sign = EnsureComp<SignatureComponent>(paper);
        sign.Data = pen.Comp.CopiedSignature;
        Dirty(paper.Owner, sign);

        UpdateUI(paper, user, true);
    }
}
