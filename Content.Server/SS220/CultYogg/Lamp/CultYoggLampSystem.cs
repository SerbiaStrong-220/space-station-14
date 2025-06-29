// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Content.Shared.SS220.CultYogg.Lamp;
using Content.Shared.SS220.StealthProvider;
using Content.Shared.Materials;

namespace Content.Server.SS220.CultYogg.Lamp;
public sealed class CultYoggLampSystem : SharedCultYoggLampSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggLampComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CultYoggLampComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CultYoggLampComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<CultYoggLampComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<CultYoggLampComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnMapInit(Entity<CultYoggLampComponent> ent, ref MapInitEvent args)
    {
        var component = ent.Comp;
        _actionContainer.EnsureAction(ent, ref component.ToggleActionEntity, component.ToggleAction);
        _actions.AddAction(ent, ref component.SelfToggleActionEntity, component.ToggleAction);
    }


    private void OnShutdown(Entity<CultYoggLampComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent, ent.Comp.ToggleActionEntity);
        _actions.RemoveAction(ent, ent.Comp.SelfToggleActionEntity);
    }

    private void OnGetActions(Entity<CultYoggLampComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
    }

    private void OnToggleAction(Entity<CultYoggLampComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Activated)
            TurnOff(ent);
        else
            TurnOn(args.Performer, ent);

        args.Handled = true;
    }

    private void OnActivate(Entity<CultYoggLampComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || !ent.Comp.ToggleOnInteract)
            return;

        if (ToggleStatus(args.User, ent))
            args.Handled = true;
    }
    public bool ToggleStatus(EntityUid user, Entity<CultYoggLampComponent> ent)
    {
        return ent.Comp.Activated ? TurnOff(ent) : TurnOn(user, ent);
    }
    public override bool TurnOff(Entity<CultYoggLampComponent> ent)
    {
        if (!ent.Comp.Activated || !_lights.TryGetLight(ent, out var pointLightComponent))
        {
            return false;
        }

        _lights.SetEnabled(ent, false, pointLightComponent);
        SetActivated(ent, false);

        var ev = new StealthProviderStatusChanged(false);
        RaiseLocalEvent(ent, ref ev);

        return true;
    }

    public override bool TurnOn(EntityUid user, Entity<CultYoggLampComponent> ent)
    {
        if (ent.Comp.Activated || !_lights.TryGetLight(ent, out var pointLightComponent))
        {
            return false;
        }

        _lights.SetEnabled(ent, true, pointLightComponent);
        SetActivated(ent, true);

        var ev = new StealthProviderStatusChanged(true);
        RaiseLocalEvent(ent, ref ev);

        return true;
    }

    public void SetActivated(Entity<CultYoggLampComponent> ent, bool activated)
    {
        if (ent.Comp.Activated == activated)
            return;

        ent.Comp.Activated = activated;

        var sound = ent.Comp.Activated ? ent.Comp.TurnOnSound : ent.Comp.TurnOffSound;
        _audio.PlayPvs(sound, ent);

        Dirty(ent, ent.Comp);
        UpdateVisuals(ent);
    }
}
