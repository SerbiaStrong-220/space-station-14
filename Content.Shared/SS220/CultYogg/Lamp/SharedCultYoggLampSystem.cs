// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.Lamp;
public abstract class SharedCultYoggLampSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly ClothingSystem _clothingSys = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggLampComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CultYoggLampComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerb);
    }

    private void OnInit(Entity<CultYoggLampComponent> ent, ref ComponentInit args)
    {
        UpdateVisuals(ent);
        Dirty(ent, ent.Comp);
    }

    public void UpdateVisuals(Entity<CultYoggLampComponent> ent)
    {
        if (ent.Comp.AddPrefix)
        {
            var prefix = ent.Comp.Activated ? "on" : "off";
            _itemSys.SetHeldPrefix(ent, prefix);
            _clothingSys.SetEquippedPrefix(ent, prefix);
        }

        if (ent.Comp.ToggleActionEntity != null)
            _action.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.Activated);

        _appearance.SetData(ent, ToggleableVisuals.Enabled, ent.Comp.Activated);
    }

    private void AddToggleLightVerb(Entity<CultYoggLampComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.ToggleOnInteract)
            return;

        var @event = args;
        ActivationVerb verb = new()
        {
            Text = Loc.GetString("verb-common-toggle-light"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = ent.Comp.Activated
                ? () => TurnOff(ent)
                : () => TurnOn(@event.User, ent)
        };

        args.Verbs.Add(verb);
    }
    public abstract bool TurnOff(Entity<CultYoggLampComponent> ent);
    public abstract bool TurnOn(EntityUid user, Entity<CultYoggLampComponent> uid);
}
