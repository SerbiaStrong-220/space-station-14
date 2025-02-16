// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Events;
using Content.Shared.Tools.Systems;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared.SS220.ModuleFurniture.Systems;

public abstract partial class SharedModuleFurnitureSystem<T> : EntitySystem where T : SharedModuleFurnitureComponent
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    private const float DoAfterMovementThreshold = 0.15f;

    // TODO: best decision is to make storage interaction event for such things
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, InteractUsingEvent>(OnInteractUsing);
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

    private void OnInteractUsing(Entity<T> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ModuleFurniturePartComponent>(args.Used))
            args.Handled = TryInsert((entity.Owner, entity.Comp), args.Used, args.User);

        if (entity.Comp.CachedLayout.Count == 0)
            args.Handled = _tool.UseTool(args.Used, args.User, entity.Owner,
                            entity.Comp.DeconstructDelaySeconds, entity.Comp.DeconstructTool,
                            new DeconstructFurnitureEvent());
        else
            args.Handled = _tool.UseTool(args.Used, args.User, entity.Owner,
                            entity.Comp.DeconstructDelaySeconds, entity.Comp.DeconstructTool,
                            new RemoveFurniturePartEvent());
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
