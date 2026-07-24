// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Events;
using Content.Shared.Tools.Systems;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Item;
using Robust.Shared.Containers;


namespace Content.Shared.SS220.ModuleFurniture.Systems;

public abstract partial class SharedModuleFurnitureSystem<T> : EntitySystem where T : SharedModuleFurnitureComponent
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const float DoAfterMovementThreshold = 0.15f;

    // TODO: best decision is to make storage interaction event for such things
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleFurniturePartComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
        SubscribeLocalEvent<ModuleFurniturePartComponent, AccessibleOverrideEvent<OpenBoundInterfaceMessage>>(OnAccessingBUI);
        SubscribeLocalEvent<ModuleFurniturePartComponent, AccessibleOverrideEvent<ActivateInWorldEvent>>(OnAccessingActivateInWorld);
        SubscribeLocalEvent<ModuleFurniturePartComponent, AccessibleOverrideEvent<InteractHandEvent>>(OnAccessingInteractHandEvent);

        SubscribeLocalEvent<ModuleFurniturePartComponent, BoundUIOpenedEvent>(OnPartBUIOpened);
        SubscribeLocalEvent<ModuleFurniturePartComponent, BoundUIClosedEvent>(OnPartBUIClosed);
    }

    public bool CanInsert(Entity<SharedModuleFurnitureComponent?> entity, Entity<ModuleFurniturePartComponent?> target, [NotNullWhen(true)] out Vector2i? offset, out string reasonLocPath)
    {
        reasonLocPath = "";
        offset = null;
        if (!Resolve(entity.Owner, ref entity.Comp) || !Resolve(target.Owner, ref target.Comp))
            return false;

        if (!EqualWidthPixel(entity.Comp, target.Comp))
        {
            reasonLocPath = "module-furniture-incorrect-size";
            return false;
        }

        if (!TryGetOffsetForPlacement(entity.Comp, target.Comp, out offset))
        {
            reasonLocPath = "module-furniture-cant-find-offset";
            return false;
        }

        return true;
    }

    public bool TryInsert(Entity<SharedModuleFurnitureComponent?> entity, Entity<ModuleFurniturePartComponent?> target, EntityUid user)
    {
        if (!CanInsert(entity, target, out var offset, out _))
            return false;

        var doafterArgs = new DoAfterArgs(EntityManager, user,
            TimeSpan.FromSeconds(2), // qol picked number
            new InsertedFurniturePart(offset.Value), entity.Owner, entity.Owner, target.Owner)
        {
            NeedHand = true,
            BreakOnMove = true,
            MovementThreshold = DoAfterMovementThreshold,
        };

        return _doAfter.TryStartDoAfter(doafterArgs);
    }

    private void OnGettingPickedUpAttempt(Entity<ModuleFurniturePartComponent> entity, ref GettingPickedUpAttemptEvent args)
    {
        // We dont want to pick up it for now.
        args.Cancel();
    }

    private void OnAccessingBUI(Entity<ModuleFurniturePartComponent> entity, ref AccessibleOverrideEvent<OpenBoundInterfaceMessage> args)
    {
        if (args.Target != entity.Owner)
            return;

        args.Accessible = true;
        args.Handled = true;
    }

    private void OnAccessingActivateInWorld(Entity<ModuleFurniturePartComponent> entity, ref AccessibleOverrideEvent<ActivateInWorldEvent> args)
    {
        if (args.Target != entity.Owner)
            return;

        args.Accessible = true;
        args.Handled = true;
    }

    private void OnAccessingInteractHandEvent(Entity<ModuleFurniturePartComponent> entity, ref AccessibleOverrideEvent<InteractHandEvent> args)
    {
        if (args.Target != entity.Owner)
            return;

        args.Accessible = true;
        args.Handled = true;
    }

    private void OnPartBUIOpened(Entity<ModuleFurniturePartComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not StorageComponent.StorageUiKey)
            return;

        _appearance.SetData(entity.Owner, ModuleFurniturePartVisuals.Opened, true);
    }

    private void OnPartBUIClosed(Entity<ModuleFurniturePartComponent> entity, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is StorageComponent.StorageUiKey)
            _appearance.SetData(entity.Owner, ModuleFurniturePartVisuals.Opened, false);
    }

    private bool EqualWidthPixel(SharedModuleFurnitureComponent furnitureComp, ModuleFurniturePartComponent partComp)
    {
        var partSize = partComp.ContainerSize;

        var resultVector = partComp.SpriteSize / furnitureComp.PixelPerLayoutTile;
        var remainderVector = partComp.SpriteSize - resultVector * furnitureComp.PixelPerLayoutTile;

        if (remainderVector != Vector2i.Zero || partSize != resultVector)
            return false;

        return true;
    }
}
