using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment;
using Content.Shared.Actions;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Weapons;
using Content.Shared.SS220.DarkForces.Ratvar.UI;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Wieldable.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using RatvarEnchantmentableComponent =
    Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.RatvarEnchantmentableComponent;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

public sealed partial class RatvarAbilitiesSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarAbilitiesComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RatvarAbilitiesComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<RatvarAbilitiesComponent, RatvarMagicEvent>(OnClockMagic);

        SubscribeLocalEvent<RatvarEnchantmentableComponent, GetEnchantmentRadialEvent>(OnGetEnchantments);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, RatvarEnchantmentSelectedMessage>(
            OnEnchantmentSelectedMessage);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, GetItemActionsEvent>(OnGetActions);

        SubscribeLocalEvent<RatvarEnchantmentableComponent, RatvarHammerKnockOffEvent>(RelayRatvarEnchantmentableEvent);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, RatvarSpearConfusionEvent>(RelayRatvarEnchantmentableEvent);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, RatvarSpearElectricalTouchEvent>(
            RelayRatvarEnchantmentableEvent);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, RatvarSwordSwordsmanEvent>(RelayRatvarEnchantmentableEvent);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, RatvarSwordBloodshedEvent>(RelayRatvarEnchantmentableEvent);

        InitMidasTouch();
        InitializeSlab();
        InitializeSpear();
        InitializeSword();
        InitializeHammer();
        InitializeShard();
    }

    private void RelayRatvarEnchantmentableEvent<T>(EntityUid uid, RatvarEnchantmentableComponent component, T args)
        where T : IRatvarAbilityRelay
    {
        var ev = new RatvarAbilityWrapper<T>(args);
        RaiseLocalEvent(uid, ev);

        component.IsEnchantmentActive = true;
        component.DisableAbilityTick = _gameTiming.CurTime + args.UseTime;

        _actionsSystem.RemoveAction(component.ActionEntity);

        var delay = EnsureComp<UseDelayComponent>(uid);
        _useDelay.SetLength((uid, delay), args.UseTime);
        _useDelay.TryResetDelay((uid, delay));
    }

    private void OnComponentInit(EntityUid uid, RatvarAbilitiesComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionMidasTouchEntity, component.ActionMidasTouch);
        _actionsSystem.AddAction(uid, ref component.ActionClockMagicEntity, component.ActionClockMagic);
    }

    private void OnComponentShutdown(EntityUid uid, RatvarAbilitiesComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionMidasTouchEntity);
        _actionsSystem.RemoveAction(uid, component.ActionClockMagicEntity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<RatvarEnchantmentableComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsEnchantmentActive || component.DisableAbilityTick > curTime)
                continue;

            component.IsEnchantmentActive = false;
            component.ActionId = null;
            component.ActionEntity = null;
            component.ActiveVisuals = string.Empty;

            _appearance.SetData(uid, RatvarEnchantmentableVisuals.State, RatvarEnchantmentableOverlays.None);
        }
    }

    private void OnGetActions(EntityUid uid, RatvarEnchantmentableComponent component, GetItemActionsEvent args)
    {
        if (!args.InHands || component.ActionId == null)
            return;

        args.AddAction(ref component.ActionEntity, component.ActionId);
    }

    private void OnEnchantmentSelectedMessage(EntityUid uid, RatvarEnchantmentableComponent component,
        RatvarEnchantmentSelectedMessage args)
    {
        component.ActionId = args.Id;

        if (Enum.TryParse<RatvarEnchantmentableOverlays>(args.Visuals, out var visuals))
        {
            component.ActiveVisuals = args.Visuals;
            _appearance.SetData(uid, RatvarEnchantmentableVisuals.State, visuals);
        }
    }

    private void OnGetEnchantments(EntityUid uid, RatvarEnchantmentableComponent component,
        ref GetEnchantmentRadialEvent args)
    {
        if (component.Enchantments.Count == 0 || component.ActionId != null)
            return;

        if (!_ui.HasUi(uid, RatvarEnchantmentUIKey.Key))
            return;

        var uiModels = new List<EnchantmentUIModel>();
        foreach (var enchantment in component.Enchantments)
        {
            var model = new EnchantmentUIModel(
                enchantment.Action,
                enchantment.Name,
                enchantment.Visuals,
                enchantment.Icon
            );

            uiModels.Add(model);
        }

        _ui.SetUiState(uid, RatvarEnchantmentUIKey.Key, new RatvarEnchantmentBUIState(uiModels));
        _ui.OpenUi(uid, RatvarEnchantmentUIKey.Key, args.Performer);
    }

    private void OnClockMagic(EntityUid uid, RatvarAbilitiesComponent component, RatvarMagicEvent args)
    {
        if (args.Handled)
            return;

        var ev = new GetEnchantmentRadialEvent(args.Performer);
        RaiseLocalEvent(args.Target, ref ev);
    }

    private bool IsWeaponWieldable(EntityUid weapon)
    {
        return TryComp<WieldableComponent>(weapon, out var wieldable) && wieldable.Wielded;
    }

    private bool IsEnchantmentActive(EntityUid item, out string enchantment)
    {
        if (!TryComp<RatvarEnchantmentableComponent>(item, out var component))
        {
            enchantment = string.Empty;
            return false;
        }

        enchantment = component.ActionId ?? string.Empty;
        return component.IsEnchantmentActive;
    }
}
