using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress;
using Content.Server.Materials;
using Content.Shared.DoAfter;
using Content.Shared.SS220.DarkForces.Ratvar.UI;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using RatvarWorkshopDoAfter = Content.Shared.SS220.DarkForces.Ratvar.DoAfterEvents.RatvarWorkshopDoAfter;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Workshop;

public sealed class RatvarWorkshopSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly RatvarProgressSystem _ratvar = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarworkShopComponent, AfterActivatableUIOpenEvent>(AfterUiOpenEvent);
        SubscribeLocalEvent<RatvarworkShopComponent, RatvarWorkshopCraftSelected>(OnCraftSelected);
        SubscribeLocalEvent<RatvarworkShopComponent, RatvarWorkshopDoAfter>(OnWorkshopDoAfter);
    }

    private void OnWorkshopDoAfter(EntityUid uid, RatvarworkShopComponent component, RatvarWorkshopDoAfter args)
    {
        if (args.Cancelled || args.Handled)
        {
            component.InProgress = false;
            return;
        }

        var transform = Transform(uid);
        Spawn(args.EntityProduce, transform.Coordinates);
        component.InProgress = false;
        UpdateUiState(uid, component);
    }

    private void OnCraftSelected(EntityUid uid, RatvarworkShopComponent component, RatvarWorkshopCraftSelected args)
    {
        if (!_material.TryChangeMaterialAmount(uid, component.RequiredMaterial, -args.Brass) || !_ratvar.TryRequestChangePower(-args.Power))
            return;

        var doAfterEvent = new RatvarWorkshopDoAfter
        {
            EntityProduce = args.EntityProduce
        };

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.Actor,
            TimeSpan.FromSeconds(args.CraftTime),
            doAfterEvent,
            uid,
            null,
            uid
        )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        component.InProgress = true;
        UpdateUiState(uid, component);
    }

    private void AfterUiOpenEvent(EntityUid uid, RatvarworkShopComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUiState(uid, component);
    }

    private void UpdateUiState(EntityUid uid, RatvarworkShopComponent component)
    {
        if (!_ui.HasUi(uid, RatvarWorkshopKey.Key))
            return;

        var brassCount = _material.GetMaterialAmount(uid, component.RequiredMaterial);
        var power = _ratvar.GetCurrentPower();

        var state = new RatvarWorkshopUIState(brassCount, power, component.InProgress);
        _ui.SetUiState(uid, RatvarWorkshopKey.Key, state);
    }
}
