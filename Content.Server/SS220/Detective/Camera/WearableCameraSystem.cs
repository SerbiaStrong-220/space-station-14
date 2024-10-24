// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SurveillanceCamera;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;

namespace Content.Server.SS220.Detective.Camera;

public sealed class WearableCameraSystem : EntitySystem
{
    [Dependency] private readonly SurveillanceCameraSystem _cameraSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WearableCameraComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<WearableCameraComponent, DetectiveCameraToggledEvent>(OnCameraToggled);
        SubscribeLocalEvent<WearableCameraComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<WearableCameraComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnComponentStartup(Entity<WearableCameraComponent> entity, ref ComponentStartup args)
    {
        if (!TryComp<SurveillanceCameraComponent>(entity, out var camera))
            return;

        _cameraSystem.SetActive(entity, false, camera);
    }

    private void OnCameraToggled(Entity<WearableCameraComponent> entity, ref DetectiveCameraToggledEvent args)
    {
        Toggle(entity);
    }

    private void OnEquipped(Entity<WearableCameraComponent> entity, ref ClothingGotEquippedEvent args)
    {
        Toggle(entity, true);
    }

    private void OnUnequipped(Entity<WearableCameraComponent> entity, ref ClothingGotUnequippedEvent args)
    {
        Toggle(entity, false);
    }

    private void Toggle(Entity<WearableCameraComponent> entity, bool? isEquiped = null)
    {
        if (!TryComp<SurveillanceCameraComponent>(entity, out var cameraComponent))
            return;
        if (!TryComp<DetectiveCameraComponent>(entity, out var detectiveCamera))
            return;
        if (!TryComp<ClothingComponent>(entity, out var clothing))
            return;

        if (isEquiped is not { } isEquipedReal)
            isEquipedReal = clothing.InSlot is not null;

        var isActive = detectiveCamera.Enabled && isEquipedReal;
        _cameraSystem.SetActive(entity, isActive, cameraComponent);
    }
}
