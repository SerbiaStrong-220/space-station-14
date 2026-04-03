using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ComponentInit>(OnInit); //SS220 Add Multifaze gun
        SubscribeLocalEvent<BatteryWeaponFireModesComponent, GunRefreshModifiersEvent>(OnRefreshModifiers); //SS220 Add Multifaze gun
    }

    private void OnExamined(Entity<BatteryWeaponFireModesComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.FireModes.Count < 2)
            return;

        var fireMode = GetMode(ent.Comp);

        //SS220 Add Multifaze gun begin
        if (!_prototypeManager.TryIndex<EntityPrototype>(fireMode.Prototype, out var proto))
            return;

        args.PushMarkup(Loc.GetString("gun-set-fire-mode-examine", ("mode", proto.Name)));
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
                text = fireMode.FireModeName ?? entProto.Name;
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
                    TrySetFireMode((uid, component), index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnUseInHandEvent(Entity<BatteryWeaponFireModesComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryCycleFireMode(ent, args.User);
    }

    public void TryCycleFireMode(Entity<BatteryWeaponFireModesComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.FireModes.Count < 2)
            return;

        var index = (ent.Comp.CurrentFireMode + 1) % ent.Comp.FireModes.Count;
        TrySetFireMode(ent, index, user);
    }

    public bool TrySetFireMode(Entity<BatteryWeaponFireModesComponent> ent, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= ent.Comp.FireModes.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, ent))
            return false;

        SetFireMode(ent, index, user);

        return true;
    }

    private void SetFireMode(Entity<BatteryWeaponFireModesComponent> ent, int index, EntityUid? user = null)
    {
        var fireMode = ent.Comp.FireModes[index];
        ent.Comp.CurrentFireMode = index;
        Dirty(ent);

        //SS220 Add Multifaze gun begin
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

        if (_prototypeManager.TryIndex(fireMode.Prototype, out var entProto))
        {
            if (TryComp<AppearanceComponent>(ent, out var appearance))
                _appearanceSystem.SetData(ent, BatteryWeaponFireModeVisuals.State, entProto.ID, appearance);

            if (user != null && fireMode.FireModeName != null)
                _popupSystem.PopupClient(Loc.GetString("gun-set-fire-mode-popup", ("mode", Loc.GetString(fireMode.FireModeName))), ent, user.Value);
        }

        if (TryComp(ent, out BatteryAmmoProviderComponent? batteryAmmoProviderComponent))
        {
            batteryAmmoProviderComponent.Prototype = fireMode.Prototype;
            batteryAmmoProviderComponent.FireCost = fireMode.FireCost;

            Dirty(ent, batteryAmmoProviderComponent);

            _gun.UpdateShots((ent, batteryAmmoProviderComponent));
        }
    }
    //SS220 Add Multifaze gun end

    //SS220 Add Multifaze gun begin
    private void OnInit(Entity<BatteryWeaponFireModesComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.FireModes.Count <= 0)
            return;

        var index = ent.Comp.CurrentFireMode % ent.Comp.FireModes.Count;
        SetFireMode(ent, index);
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
