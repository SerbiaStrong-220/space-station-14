// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Access.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Contraband;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ChameleonStructure;

public abstract class SharedChameleonStructureSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> WhitelistChameleonTag = "WhitelistChameleon";

    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStructureComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
    }

    private void OnVerb(Entity<ChameleonStructureComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.UserWhitelist is not null && !_whitelist.IsValid(ent.Comp.UserWhitelist, args.User))
            return;

        // Can't pass args from a ref event inside of lambdas
        var user = args.User;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("chameleon-component-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => UI.TryToggleUi(ent.Owner, ChameleonUiKey.Key, user)
        });
    }
    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto) { }
}
