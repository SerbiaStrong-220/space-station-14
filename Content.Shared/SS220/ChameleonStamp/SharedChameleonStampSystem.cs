// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Paper;
using Content.Shared.Prototypes;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ChameleonStamp;

public abstract partial class SharedChameleonStampSystem : EntitySystem
{
    [Dependency] protected SharedUserInterfaceSystem UI = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStampComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
    }

    private void OnVerb(Entity<ChameleonStampComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<StampComponent>(ent))
            return;

        var user = args.User;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("chameleon-stamp-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => UI.TryToggleUi(ent.Owner, ChameleonStampUiKey.Key, user)
        });
    }

    private void UpdateInk(Entity<ChameleonStampComponent> ent, EntityPrototype proto)
    {
        if (!proto.TryGetComponent(out StampComponent? other, Factory))
            return;

        if (!TryComp<StampComponent>(ent, out var stamp))
            return;

        stamp.StampedName = other.StampedName;
        stamp.StampState = other.StampState;
        stamp.StampedColor = other.StampedColor;
    }

    protected virtual void UpdateSprite(Entity<ChameleonStampComponent> ent, EntityPrototype proto) { }

    protected void UpdateVisuals(Entity<ChameleonStampComponent> ent)
    {
        if (ent.Comp.Prototype is null)
            return;

        if (!_proto.TryIndex(ent.Comp.Prototype, out var proto))
            return;

        UpdateInk(ent, proto);
        UpdateSprite(ent, proto);
    }

    private bool IsStamp(EntityPrototype proto)
    {
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        return proto.HasComponent<StampComponent>(Factory);
    }

    private bool IsValidProto(EntityPrototype proto, List<EntProtoId> allowed)
    {
        return IsStamp(proto) && allowed.Contains(proto.ID);
    }

    private void UpdateUi(Entity<ChameleonStampComponent> ent)
    {
        var state = new ChameleonStampBoundUserInterfaceState(ent.Comp.Prototype, GetValidVariants(ent));
        UI.SetUiState(ent.Owner, ChameleonStampUiKey.Key, state);
    }

    private List<EntProtoId> GetValidVariants(Entity<ChameleonStampComponent> ent)
    {
        var result = new List<EntProtoId>(ent.Comp.Variants.Count);

        foreach (var protoId in ent.Comp.Variants)
        {
            if (result.Contains(protoId))
                continue;

            if (!_proto.TryIndex(protoId, out var proto))
                continue;

            if (!IsStamp(proto))
                continue;

            result.Add(protoId);
        }

        return result;
    }

    public bool TrySetPrototype(Entity<ChameleonStampComponent> ent, EntProtoId? protoId, bool forceUpdate = false)
    {
        if (protoId is null)
            return false;

        if (ent.Comp.Prototype == protoId && !forceUpdate)
            return false;

        if (!_proto.TryIndex(protoId, out var proto))
            return false;

        if (!IsValidProto(proto, ent.Comp.Variants))
            return false;

        ent.Comp.Prototype = protoId;
        UpdateVisuals(ent);

        UpdateUi(ent);
        Dirty(ent, ent.Comp);

        return true;
    }
}