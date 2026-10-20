using Content.Server.SS220.DarkForces.Narsi.Cultist.Gear;
using Content.Server.Materials;
using Content.Shared.DoAfter;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.SS220.Cult;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.SS220.DarkForces.Narsi.Buildings;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Forge;

public sealed class NarsiCultForgeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiCultForgeComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<NarsiCultForgeComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);
        SubscribeLocalEvent<NarsiCultForgeComponent, NarsiForgeDoAfterEvent>(OnForgeDoAfter);
        SubscribeLocalEvent<NarsiCultForgeComponent, ComponentInit>(OnForgeInit);
        SubscribeLocalEvent<NarsiCultForgeComponent, NarsiForgeCreateItemEvent>(OnCreateItemEvent);
        SubscribeLocalEvent<NarsiCultForgeComponent, MaterialEntityInsertedEvent>(OnMaterialInsert);
    }

    private void OnOpenUIAttempt(EntityUid uid, NarsiCultForgeComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (HasComp<NarsiCultistComponent>(args.User))
            return;

        args.Cancel();
    }

    private void OnBeforeUIOpen(EntityUid uid, NarsiCultForgeComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateState(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var forges = EntityQueryEnumerator<NarsiCultForgeComponent>();
        while (forges.MoveNext(out var uid, out var forgeComponent))
        {
            if (forgeComponent.State != NarsiForgeState.Delay || forgeComponent.DelayTick > curTime)
                continue;

            forgeComponent.State = NarsiForgeState.Idle;
            UpdateState(uid, forgeComponent);
        }
    }

    private void OnMaterialInsert(EntityUid uid, NarsiCultForgeComponent component,
        ref MaterialEntityInsertedEvent args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, NarsiCultForgeComponent component)
    {
        var runicPlasteelCount = _material.GetMaterialAmount(uid, component.RunicPlasteel);
        var plasteelCount = _material.GetMaterialAmount(uid, component.Plasteel);
        var steelCount = _material.GetMaterialAmount(uid, component.Steel);

        _ui.SetUiState(
            uid,
            SharedNarsiForgeInterfaceKey.Key,
            new NarsiForgeUIState(component.State, runicPlasteelCount, plasteelCount, steelCount)
        );

        _appearance.SetData(uid, NarsiForgeVisuals.State, component.State);
    }

    private void OnCreateItemEvent(EntityUid uid, NarsiCultForgeComponent component, NarsiForgeCreateItemEvent args)
    {
        var requiredMaterialCount = _material.GetMaterialAmount(uid, args.RequiredMaterial);
        var actualCost = args.Cost;
        if (requiredMaterialCount < actualCost)
        {
            _popup.PopupEntity("Недостаточно материалов", uid);
            return;
        }

        if (!_material.TryChangeMaterialAmount(uid, args.RequiredMaterial, -actualCost))
            return;

        var doAfterEvent = new NarsiForgeDoAfterEvent(args.ItemPrototype, GetNetEntity(uid));
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.Actor,
            component.DoAfterDelay,
            doAfterEvent,
            uid,
            null,
            uid
        )
        {
            BreakOnMove = true,
            MovementThreshold = 1.0f
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        _audioSystem.PlayPvs(component.ForgeSound, uid, component.ForgeSoundParams);
        component.State = NarsiForgeState.Working;
        UpdateState(uid, component);
    }

    private void OnForgeInit(EntityUid uid, NarsiCultForgeComponent component, ComponentInit args)
    {
        var storageComp = EnsureComp<MaterialStorageComponent>(uid);

        _material.TryChangeMaterialAmount(uid, component.RunicPlasteel, 0, storageComp);
        _material.TryChangeMaterialAmount(uid, component.Steel, 0, storageComp);
        _material.TryChangeMaterialAmount(uid, component.Plasteel, 0, storageComp);
    }

    private void OnForgeDoAfter(EntityUid uid, NarsiCultForgeComponent component, NarsiForgeDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            component.State = NarsiForgeState.Idle;
            UpdateState(uid, component);

            args.Handled = true;
            return;
        }

        var coordinates = Transform(GetEntity(args.SourceEntityUid)).Coordinates;
        var cultistGear = Spawn(args.EntityToSpawn, coordinates);

        EnsureComp<NarsiCultistGearComponent>(cultistGear);

        component.State = NarsiForgeState.Delay;
        component.DelayTick = _timing.CurTime + component.DelayThreshold;

        UpdateState(uid, component);
        args.Handled = true;
    }
}
