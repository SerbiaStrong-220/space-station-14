// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using System;
using System.ComponentModel;

namespace Content.Shared.SS220.CultYogg.Lamp;
public abstract class SharedCultYoggLampSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

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
        if (ent.Comp.AddPrefix)
        {
            var prefix = ent.Comp.Activated ? "on" : "off";
            _itemSys.SetHeldPrefix(ent, prefix);
            _clothingSys.SetEquippedPrefix(ent, prefix);
        }

        if (ent.Comp.ToggleActionEntity != null)
            _action.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.Activated);

        //_appearance.SetData(ent, ToggleableLightVisuals.Enabled, component.Activated, appearanc

        // Want to make sure client has latest data on level so battery displays properly.
        Dirty(ent, ent.Comp);
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
    public abstract bool TurnOff(Entity<CultYoggLampComponent> ent, bool makeNoise = true);
    public abstract bool TurnOn(EntityUid user, Entity<CultYoggLampComponent> uid);
}
