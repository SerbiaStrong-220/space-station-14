using System.Linq;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!; //SS220 Add Multifaze gun
    [Dependency] private readonly SharedGunSystem _gunSystem = default!; //SS220 Add Multifaze gun

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ActivateInWorldEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ComponentInit>(OnInit); //SS220 Add Multifaze gun
    }

    private void OnExamined(EntityUid uid, BatteryWeaponFireModesComponent component, ExaminedEvent args)
    {
        if (component.FireModes.Count < 2)
            return;

        var fireMode = GetMode(component);
        var name = string.Empty; //SS220 Add Multifaze gun

        //SS220 Add Multifaze gun begin
        //if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
        //    return;

        if (fireMode.FireModeName != null)
            name = fireMode.FireModeName;
        else if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var entProto))
            name = entProto.Name;
        //SS220 Add Multifaze gun end

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", name))); //SS220 Add Multifaze gun
    }

    private BatteryWeaponFireMode GetMode(BatteryWeaponFireModesComponent component)
    {
        return component.FireModes[component.CurrentFireMode];
    }

    private void OnGetVerb(EntityUid uid, BatteryWeaponFireModesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (component.FireModes.Count < 2)
            return;

        for (var i = 0; i < component.FireModes.Count; i++)
        {
            var fireMode = component.FireModes[i];
            // var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype); //SS220 Add Multifaze gun
            var index = i;

            //SS220 Add Multifaze gun begin
            var text = string.Empty;

            if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var entProto))
            {
                if (fireMode.FireModeName is not null)
                    text = fireMode.FireModeName;
                else
                    text = entProto.Name;
            }
            else if (_prototypeManager.TryIndex<HitscanPrototype>(fireMode.Prototype, out _))
            {
                text += fireMode.FireModeName;
            }
            else
            {
                return;
            }
            //SS220 Add Multifaze gun end

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = text, //SS220 Add Multifaze gun
                Disabled = i == component.CurrentFireMode, 
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetFireMode(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnInteractHandEvent(EntityUid uid, BatteryWeaponFireModesComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        if (component.FireModes.Count < 2)
            return;

        CycleFireMode(uid, component, args.User);
    }

    private void CycleFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, EntityUid user)
    {
        if (component.FireModes.Count < 2)
            return;

        var index = (component.CurrentFireMode + 1) % component.FireModes.Count;
        SetFireMode(uid, component, index, user);
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);

        //SS220 Add Multifaze gun begin
        var name = string.Empty;

        //if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider))
        //{
        //    if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
        //        return;

        //    projectileBatteryAmmoProvider.Prototype = fireMode.Prototype;
        //    projectileBatteryAmmoProvider.FireCost = fireMode.FireCost;
        //}

        if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var entProto))
        {
            {
                if (HasComp<HitscanBatteryAmmoProviderComponent>(uid))
                    RemComp<HitscanBatteryAmmoProviderComponent>(uid);

                if (!_gameTiming.ApplyingState)
                    EnsureComp<ProjectileBatteryAmmoProviderComponent>(uid);

                if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProvider))
                {

                    if (fireMode.FireModeName is not null)
                        name = fireMode.FireModeName;
                    else
                        name = entProto.Name;

                    projectileBatteryAmmoProvider.Prototype = fireMode.Prototype;
                    projectileBatteryAmmoProvider.FireCost = fireMode.FireCost;

                    Dirty(uid, projectileBatteryAmmoProvider);
                }
            }
        }
        else if (_prototypeManager.TryIndex<HitscanPrototype>(fireMode.Prototype, out _))
        {
            {
                if (HasComp<ProjectileBatteryAmmoProviderComponent>(uid))
                    RemComp<ProjectileBatteryAmmoProviderComponent>(uid);

                if (!_gameTiming.ApplyingState)
                    EnsureComp<HitscanBatteryAmmoProviderComponent>(uid);

                if (TryComp(uid, out HitscanBatteryAmmoProviderComponent? hitscanBatteryAmmoProvider))
                {
                    name += fireMode.FireModeName;

                    hitscanBatteryAmmoProvider.Prototype = fireMode.Prototype;
                    hitscanBatteryAmmoProvider.FireCost = fireMode.FireCost;

                    Dirty(uid, hitscanBatteryAmmoProvider);
                }
            }
        }
        else
        {
            Log.Error($"{fireMode.Prototype} is not Entity or Hitscan prototype");
            return;
        }

        if (fireMode.SoundGunshot is not null)
            _gunSystem.SetSoundGunshot(uid, fireMode.SoundGunshot);
        //SS220 Add Multifaze gun end

        if (user != null)
        {
            _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", name)), uid, user.Value); //SS220 Add Multifaze gun
        }

        //SS220 Add Multifaze gun begin
        var ev = new ChangeFireModeEvent(uid, component, index);
        RaiseLocalEvent(uid, ref ev);
        //SS220 Add Multifaze gun end
    }

    //SS220 Add Multifaze gun begin
    private void OnInit(EntityUid uid, BatteryWeaponFireModesComponent component, ref ComponentInit args)
    {
        SetFireMode(uid, component, component.CurrentFireMode);
    }

    [ByRefEvent]
    public record struct ChangeFireModeEvent(EntityUid Uid, BatteryWeaponFireModesComponent Component, int Index);
    //SS220 Add Multifaze gun end
}
