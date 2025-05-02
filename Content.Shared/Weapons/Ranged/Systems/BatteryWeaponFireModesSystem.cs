using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!; //SS220 Add Multifaze gun
    [Dependency] private readonly SharedGunSystem _gunSystem = default!; //SS220 Add Multifaze gun
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ComponentInit>(OnInit); //SS220 Add Multifaze gun
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GunRefreshModifiersEvent>(OnRefreshModifiers); //SS220 Add Multifaze gun
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

        args.PushMarkup(Loc.GetString("gun-set-fire-mode", ("mode", Loc.GetString(name)))); //SS220 Add Multifaze gun
    }

    private BatteryWeaponFireMode GetMode(BatteryWeaponFireModesComponent component)
    {
        return component.FireModes[component.CurrentFireMode];
    }

    private void OnGetVerb(EntityUid uid, BatteryWeaponFireModesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.FireModes.Count < 2)
            return;

        if (!_accessReaderSystem.IsAllowed(args.User, uid))
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
            //SS220 Add Multifaze gun end

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = Loc.GetString(text), //SS220 Add Multifaze gun
                Disabled = i == component.CurrentFireMode,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    TrySetFireMode(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnUseInHandEvent(EntityUid uid, BatteryWeaponFireModesComponent component, UseInHandEvent args)
    {
        TryCycleFireMode(uid, component, args.User);
    }

    public void TryCycleFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, EntityUid? user = null)
    {
        if (component.FireModes.Count < 2)
            return;

        var index = (component.CurrentFireMode + 1) % component.FireModes.Count;
        TrySetFireMode(uid, component, index, user);
    }

    public bool TrySetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= component.FireModes.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, uid))
            return false;

        SetFireMode(uid, component, index, user);

        return true;
    }

    private void SetFireMode(EntityUid uid, BatteryWeaponFireModesComponent component, int index, EntityUid? user = null)
    {
        var fireMode = component.FireModes[index];
        component.CurrentFireMode = index;
        Dirty(uid, component);

        //SS220 Add Multifaze gun begin
        var name = string.Empty;

        //if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var prototype))
        //{
        //    if (TryComp<AppearanceComponent>(uid, out var appearance))
        //        _appearanceSystem.SetData(uid, BatteryWeaponFireModeVisuals.State, prototype.ID, appearance);

        //    if (user != null)
        //        _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", prototype.Name)), uid, user.Value);
        //}

        //if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProviderComponent))
        //{
        //    // TODO: Have this get the info directly from the batteryComponent when power is moved to shared.
        //    var OldFireCost = projectileBatteryAmmoProviderComponent.FireCost;
        //    projectileBatteryAmmoProviderComponent.Prototype = fireMode.Prototype;
        //    projectileBatteryAmmoProviderComponent.FireCost = fireMode.FireCost;

        //    float FireCostDiff = (float)fireMode.FireCost / (float)OldFireCost;
        //    projectileBatteryAmmoProviderComponent.Shots = (int)Math.Round(projectileBatteryAmmoProviderComponent.Shots / FireCostDiff);
        //    projectileBatteryAmmoProviderComponent.Capacity = (int)Math.Round(projectileBatteryAmmoProviderComponent.Capacity / FireCostDiff);

        //    Dirty(uid, projectileBatteryAmmoProviderComponent);

        //    var updateClientAmmoEvent = new UpdateClientAmmoEvent();
        //    RaiseLocalEvent(uid, ref updateClientAmmoEvent);
        //}

        if (_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var entProto))
        {
            if (HasComp<HitscanBatteryAmmoProviderComponent>(uid))
                RemComp<HitscanBatteryAmmoProviderComponent>(uid);

            if (!_gameTiming.ApplyingState)
                EnsureComp<ProjectileBatteryAmmoProviderComponent>(uid);

            if (TryComp(uid, out ProjectileBatteryAmmoProviderComponent? projectileBatteryAmmoProviderComponent))
            {

                if (fireMode.FireModeName is not null)
                    name = fireMode.FireModeName;
                else
                    name = entProto.Name;

                // TODO: Have this get the info directly from the batteryComponent when power is moved to shared.
                var OldFireCost = projectileBatteryAmmoProviderComponent.FireCost;
                projectileBatteryAmmoProviderComponent.Prototype = fireMode.Prototype;
                projectileBatteryAmmoProviderComponent.FireCost = fireMode.FireCost;

                float FireCostDiff = (float)fireMode.FireCost / (float)OldFireCost;
                projectileBatteryAmmoProviderComponent.Shots = (int)Math.Round(projectileBatteryAmmoProviderComponent.Shots / FireCostDiff);
                projectileBatteryAmmoProviderComponent.Capacity = (int)Math.Round(projectileBatteryAmmoProviderComponent.Capacity / FireCostDiff);

                Dirty(uid, projectileBatteryAmmoProviderComponent);

                var updateClientAmmoEvent = new UpdateClientAmmoEvent();
                RaiseLocalEvent(uid, ref updateClientAmmoEvent);
            }
        }
        else if (_prototypeManager.TryIndex<HitscanPrototype>(fireMode.Prototype, out _))
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
        else
        {
            Log.Error($"{fireMode.Prototype} is not Entity or Hitscan prototype");
            return;
        }

        _gunSystem.RefreshModifiers(uid);
        //SS220 Add Multifaze gun end

        if (user != null)
        {
            _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode", ("mode", Loc.GetString(name))), uid, user.Value); //SS220 Add Multifaze gun
        }

        //SS220 Add Multifaze gun begin
        var ev = new ChangeFireModeEvent(index);
        RaiseLocalEvent(uid, ref ev);
        //SS220 Add Multifaze gun end
    }

    //SS220 Add Multifaze gun begin
    private void OnInit(Entity<BatteryWeaponFireModesComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.FireModes.Count <= 0)
            return;

        var index = ent.Comp.CurrentFireMode % ent.Comp.FireModes.Count;
        SetFireMode(ent, ent.Comp, index);
    }

    private void OnRefreshModifiers(Entity<BatteryWeaponFireModesComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var firemode = GetMode(ent.Comp);

        if (firemode.GunModifiers is not { } modifiers ||
            !TryComp<GunComponent>(ent.Owner, out var gunComponent))
            return;

        args.SoundGunshot = modifiers.SoundGunshot ?? gunComponent.SoundGunshot;
        args.AngleIncrease = modifiers.AngleIncrease ?? gunComponent.AngleIncrease;
        args.AngleDecay = modifiers.AngleDecay ?? gunComponent.AngleDecay;
        args.MaxAngle = modifiers.MaxAngle ?? gunComponent.MaxAngle;
        args.MinAngle = modifiers.MinAngle ?? gunComponent.MinAngle;
        args.ShotsPerBurst = modifiers.ShotsPerBurst ?? gunComponent.ShotsPerBurst;
        args.FireRate = modifiers.FireRate ?? gunComponent.FireRate;
        args.ProjectileSpeed = modifiers.ProjectileSpeed ?? gunComponent.ProjectileSpeed;
    }
    //SS220 Add Multifaze gun end
}

//SS220 Add Multifaze gun begin
/// <summary>
/// The event that rises when the fire mode is selected
/// </summary>
/// <param name="Index"></param>
[ByRefEvent]
public record struct ChangeFireModeEvent(int Index);
//SS220 Add Multifaze gun end
