// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Buildings;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.CultYogg.MiGo;

public sealed class SharedMiGoErectSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    private readonly List<EntityUid> _dropEntitiesBuffer = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MiGoComponent, MiGoErectBuildMessage>(OnBuildMessage);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEraseMessage>(OnEraseMessage);
        SubscribeLocalEvent<MiGoComponent, MiGoErectDoAfterEvent>(OnDoAfterErect);

        SubscribeLocalEvent<CultYoggBuildingFrameComponent, ComponentInit>(OnBuildingFrameInit);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, InteractUsingEvent>(OnBuildingFrameInteractUsing);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, GetVerbsEvent<InteractionVerb>>(AddInteractionVerbs);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, GetVerbsEvent<Verb>>(AddVerbs);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, ExaminedEvent>(OnBuildingFrameExamined);

        SubscribeLocalEvent<MiGoEraseDoAfterEvent>(OnEraseDoAfter);
    }

    public void OpenUI(Entity<MiGoComponent> entity, ActorComponent actor)
    {
        _userInterfaceSystem.TryToggleUi(entity.Owner, MiGoUiKey.Erect, actor.PlayerSession);
    }

    private void OnBuildMessage(Entity<MiGoComponent> entity, ref MiGoErectBuildMessage args)
    {
        if (entity.Owner != args.Actor)
            return;

        if (!_prototypeManager.TryIndex(args.BuildingId, out _))
            return;

        var erectAction = entity.Comp.MiGoErectActionEntity;

        if (erectAction == null || !TryComp<InstantActionComponent>(erectAction, out var actionComponent))
            return;

        if (actionComponent.Cooldown.HasValue && actionComponent.Cooldown.Value.End > _gameTiming.CurTime)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-yogg-building-cooldown-popup"), entity, entity);
            return;
        }
        var location = GetCoordinates(args.Location);
        var tileRef = location.GetTileRef();

        if (tileRef == null || _turfSystem.IsTileBlocked(tileRef.Value, Physics.CollisionGroup.MachineMask))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-yogg-building-tile-blocked-popup"), entity, entity);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, entity, TimeSpan.FromSeconds(entity.Comp.ErectDoAfterSeconds),
            new MiGoErectDoAfterEvent()
            {
                BuildingId = args.BuildingId,
                Location = args.Location,
                Direction = args.Direction,
            }, entity, null, null)
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        });
    }

    private void OnEraseMessage(Entity<MiGoComponent> entity, ref MiGoErectEraseMessage args)
    {
        if (entity.Owner != args.Actor)
            return;

        var buildingUid = EntityManager.GetEntity(args.BuildingFrame);
        if (_whitelistSystem.IsWhitelistFail(entity.Comp.EraseWhitelist, buildingUid))
            return;

        var doAfterTime = TimeSpan.Zero;
        if (TryComp<CultYoggBuildingFrameComponent>(buildingUid, out var frameComponent) &&
            frameComponent.EraseTime != null)
            doAfterTime = frameComponent.EraseTime.Value;
        else if (TryComp<CultYoggBuildingComponent>(buildingUid, out var buildingComponent) &&
            buildingComponent.EraseTime != null)
            doAfterTime = buildingComponent.EraseTime.Value;
        else
            doAfterTime = entity.Comp.BaseEraseTime;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            entity.Owner,
            doAfterTime,
            new MiGoEraseDoAfterEvent(),
            null,
            buildingUid
        )
        {
            Broadcast = true,
            BreakOnDamage = false,
            BreakOnMove = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfterErect(Entity<MiGoComponent> entity, ref MiGoErectDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (_netManager.IsClient)
            return;

        if (!_prototypeManager.TryIndex(args.BuildingId, out var buildingPrototype))
            return;

        var location = GetCoordinates(args.Location);
        if (buildingPrototype.FrameProtoId.HasValue)
        {
            PlaceBuildingFrame(buildingPrototype, location, args.Direction);
        }
        else
        {
            PlaceCompleteBuilding(buildingPrototype, location, args.Direction);
        }

        var erectAction = entity.Comp.MiGoErectActionEntity;
        if (erectAction == null || !TryComp<InstantActionComponent>(erectAction, out var actionComponent))
            return;

        var cooldown = buildingPrototype.CooldownOverride ?? actionComponent.UseDelay ?? TimeSpan.FromSeconds(1);
        _actionsSystem.SetCooldown(erectAction, cooldown);
        args.Handled = true;
    }

    private void OnBuildingFrameInit(Entity<CultYoggBuildingFrameComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Container = _containerSystem.EnsureContainer<Container>(entity, CultYoggBuildingFrameComponent.ContainerId);
    }

    private void OnBuildingFrameInteractUsing(Entity<CultYoggBuildingFrameComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryInsert(entity, args.Used))
            args.Handled = true;
    }

    private void AddInteractionVerbs(Entity<CultYoggBuildingFrameComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (args.Using == null || !_actionBlockerSystem.CanDrop(args.User))
            return;

        if (!CanInsert(entity, args.Using.Value))
            return;

        var verbSubject = Name(args.Using.Value);

        var item = args.Using.Value;
        InteractionVerb insertVerb = new()
        {
            Text = Loc.GetString("place-item-verb-text", ("subject", verbSubject)),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/drop.svg.192dpi.png")),
            IconEntity = GetNetEntity(args.Using),
            Act = () => TryInsert(entity, item)
        };

        args.Verbs.Add(insertVerb);
    }

    private void AddVerbs(Entity<CultYoggBuildingFrameComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess)
            return;

        Verb destroyVerb = new()
        {
            Text = Loc.GetString("cult-yogg-building-frame-verb-destroy"),
            Act = () => DeconstructBuilding(entity),
        };
        args.Verbs.Add(destroyVerb);
    }

    private void OnBuildingFrameExamined(Entity<CultYoggBuildingFrameComponent> entity, ref ExaminedEvent args)
    {
        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return;
        using (args.PushGroup(nameof(CultYoggBuildingFrameComponent)))
        {
            for (var i = 0; i < neededMaterials.Count; i++)
            {
                var neededMaterial = neededMaterials[i];
                var addedCount = entity.Comp.AddedMaterialsAmount[i];

                var locKey = addedCount >= neededMaterial.Count ?
                    "cult-yogg-building-frame-examined-material-full" :
                    "cult-yogg-building-frame-examined-material-needed";

                if (!_prototypeManager.TryIndex(neededMaterial.StackType, out var stackType))
                    continue;

                var materialName = Loc.GetString(stackType.Name);
                args.PushMarkup(Loc.GetString(locKey, ("material", materialName), ("currentAmount", addedCount), ("totalAmount", neededMaterial.Count)));
            }
        }
    }

    private void OnEraseDoAfter(MiGoEraseDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is { } target)
            DeconstructBuilding(target);
    }

    private Entity<CultYoggBuildingFrameComponent> PlaceBuildingFrame(CultYoggBuildingPrototype buildingPrototype, EntityCoordinates location, Direction direction)
    {
        var frameEntity = SpawnAtPosition(buildingPrototype.FrameProtoId, location);
        Transform(frameEntity).LocalRotation = direction.ToAngle();

        var resultEntityProto = _prototypeManager.Index(buildingPrototype.ResultProtoId);

        _metaDataSystem.SetEntityName(frameEntity, Loc.GetString("cult-yogg-building-frame-name-template", ("name", resultEntityProto.Name)));

        var frame = EnsureComp<CultYoggBuildingFrameComponent>(frameEntity);
        frame.BuildingPrototypeId = buildingPrototype.ID;

        while (frame.AddedMaterialsAmount.Count < buildingPrototype.Materials.Count)
        {
            frame.AddedMaterialsAmount.Add(0);
        }
        Dirty(new Entity<CultYoggBuildingFrameComponent>(frameEntity, frame));

        return (frameEntity, frame);
    }

    private EntityUid PlaceCompleteBuilding(CultYoggBuildingPrototype buildingPrototype, EntityCoordinates location, Direction direction)
    {
        var building = SpawnAtPosition(buildingPrototype.ResultProtoId, location);
        Transform(building).LocalRotation = direction.ToAngle();

        return building;
    }

    private bool CanInsert(Entity<CultYoggBuildingFrameComponent> entity, EntityUid item)
    {
        return CanInsert(entity, item, out _);
    }

    private bool CanInsert(Entity<CultYoggBuildingFrameComponent> entity, EntityUid item, out int materialIndex)
    {
        materialIndex = 0;
        if (!TryComp<StackComponent>(item, out var stack))
            return false;

        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return false;

        for (var i = 0; i < neededMaterials.Count; i++)
        {
            var materialToBuild = neededMaterials[i];
            if (stack.StackTypeId == materialToBuild.StackType)
            {
                materialIndex = i;
                return true;
            }
        }
        return false;
    }

    private bool TryInsert(Entity<CultYoggBuildingFrameComponent> entity, EntityUid item)
    {
        if (!CanInsert(entity, item, out var materialIndex))
            return false;

        if (!TryComp<StackComponent>(item, out var stack))
            return false;

        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return false;

        var materialToBuild = neededMaterials[materialIndex];
        var countToAdd = stack.Count;
        var containedCount = entity.Comp.AddedMaterialsAmount[materialIndex];
        var canAdd = Math.Min(countToAdd, materialToBuild.Count - containedCount);
        var leftCount = countToAdd - canAdd;

        if (canAdd <= 0)
            return false;

        if (_gameTiming.InPrediction)
            return true; // In prediction just say that we can, all the heavy lifting is up to server

        EntityUid materialEntityToInsert;
        if (leftCount == 0)
        {
            materialEntityToInsert = item;
        }
        else
        {
            var stackTypeProto = _prototypeManager.Index(materialToBuild.StackType);
            materialEntityToInsert = Spawn(stackTypeProto.Spawn);
            _stackSystem.SetCount(materialEntityToInsert, canAdd);

            var materialEntityToLeft = item;
            _stackSystem.SetCount(materialEntityToLeft, leftCount);
        }
        _containerSystem.Insert(materialEntityToInsert, entity.Comp.Container);
        entity.Comp.AddedMaterialsAmount[materialIndex] = containedCount + canAdd;

        Dirty(entity);

        if (IsBuildingFrameCompleted(entity))
            CompleteBuilding(entity);

        return true;
    }

    private bool TryGetNeededMaterials(Entity<CultYoggBuildingFrameComponent> entity, [NotNullWhen(true)] out List<CultYoggBuildingMaterial>? materials)
    {
        materials = null;

        if (!_prototypeManager.TryIndex(entity.Comp.BuildingPrototypeId, out var prototype))
            return false;

        materials = prototype.Materials;
        return true;
    }

    private bool IsBuildingFrameCompleted(Entity<CultYoggBuildingFrameComponent> entity)
    {
        if (!TryGetNeededMaterials(entity, out var neededMaterials))
            return false;

        for (var i = 0; i < neededMaterials.Count; i++)
        {
            var materialToBuild = neededMaterials[i];
            var addedAmount = entity.Comp.AddedMaterialsAmount[i];

            if (addedAmount < materialToBuild.Count)
                return false;
        }
        return true;
    }

    private EntityUid CompleteBuilding(Entity<CultYoggBuildingFrameComponent> entity)
    {
        if (_gameTiming.InPrediction) // this should never run in client
            return default;

        if (!_prototypeManager.TryIndex(entity.Comp.BuildingPrototypeId, out var prototype, logError: true))
            return default;

        var transform = Transform(entity);

        var resultEntity = PlaceCompleteBuilding(prototype, transform.Coordinates, transform.LocalRotation.GetDir());

        Del(entity);

        return resultEntity;
    }

    private void DeconstructBuilding(EntityUid uid)
    {
        if (_gameTiming.InPrediction)
            return; // this should never run in client

        var coords = Transform(uid).Coordinates;

        if (TryComp<CultYoggBuildingFrameComponent>(uid, out var frameComp))
        {
            var dropItems = frameComp.Container.ContainedEntities;
            foreach (var item in dropItems)
            {
                _transformSystem.AttachToGridOrMap(item);
                _transformSystem.SetCoordinates(item, coords);
            }
        }
        else if (TryComp<CultYoggBuildingComponent>(uid, out var buildingComp) &&
            buildingComp.SpawnOnErase != null)
        {
            foreach (var proto in buildingComp.SpawnOnErase)
            {
                for (var i = 1; i <= proto.Amount; i++)
                {
                    var ent = Spawn(proto.Id, coords);

                    if (proto.StackAmount is { } stackAmount)
                        _stackSystem.SetCount(ent, stackAmount);
                }
            }
        }

        Del(uid);
    }
}

[Serializable, NetSerializable]
public sealed partial class MiGoErectDoAfterEvent : SimpleDoAfterEvent
{
    public ProtoId<CultYoggBuildingPrototype> BuildingId;
    public NetCoordinates Location;
    public Direction Direction;
}

[Serializable, NetSerializable]
public sealed partial class MiGoEraseDoAfterEvent : SimpleDoAfterEvent
{
}
