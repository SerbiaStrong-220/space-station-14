// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ChameleonStructure;

public abstract class SharedChameleonStructureSystem : EntitySystem
{
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private readonly List<EntProtoId> _data = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStructureComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<ChameleonStructureComponent, PrototypesReloadedEventArgs>(OnPrototypeReload);
        UpdateData();
    }

    private void OnPrototypeReload(Entity<ChameleonStructureComponent> ent, ref PrototypesReloadedEventArgs args)
    {
        UpdateData();
    }
    private void OnVerb(Entity<ChameleonStructureComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, args.User))
            return;

        // Can't pass args from a ref event inside of lambdas
        var user = args.User;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("chameleon-component-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => UI.TryToggleUi(ent.Owner, ChameleonStructureUiKey.Key, user)
        });
    }
    protected virtual void UpdateSprite(EntityUid ent, EntityPrototype proto) { }

    protected void UpdateVisuals(Entity<ChameleonStructureComponent> ent)
    {
        if (string.IsNullOrEmpty(ent.Comp.Prototype))
            return;

        if (!_proto.TryIndex(ent.Comp.Prototype, out EntityPrototype? proto))
            return;

        UpdateSprite(ent, proto); // maybe later figure out

        var meta = MetaData(ent);
        _metaData.SetEntityName(ent, proto.Name, meta);
        _metaData.SetEntityDescription(ent, proto.Description, meta);
    }

    /// <summary>
    ///     Check if this entity prototype is valid target for chameleon item.
    /// </summary>
    public bool IsValidTarget(EntityPrototype proto, string? requiredTag = null)
    {
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        if (!proto.TryGetComponent(out TagComponent? tag, Factory))//IDK about WhitelistChameleon should it be or not
            return false;

        if (requiredTag != null && !_tag.HasTag(tag, requiredTag))
            return false;

        return true;
    }

    /// <summary>
    ///     Get a list of valid chameleon targets
    /// </summary>
    public IEnumerable<EntProtoId> GetValidTargets()
    {
        return _data;
    }

    protected void UpdateData()
    {
        _data.Clear();
        var prototypes = _proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            // check if this is valid clothing
            if (!IsValidTarget(proto))
                continue;

            _data.Add(proto.ID);
        }
    }
}
