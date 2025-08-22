// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.SS220.ChameleonStructure;
using Content.Shared.SS220.CultYogg.Buildings;
using Content.Shared.SS220.CultYogg.Corruption;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
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
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedChameleonStructureSystem _chameleonStructureSystem = default!;

    //private readonly List<EntityUid> _dropEntitiesBuffer = [];

    private readonly Dictionary<ProtoId<EntityPrototype>, MiGoCapturePrototype> _сaptureDictBySourcePrototypeId = [];
    private readonly Dictionary<ProtoId<EntityPrototype>, MiGoCapturePrototype> _сaptureDictByParentPrototypeId = [];
    private readonly Dictionary<ProtoId<TagPrototype>, MiGoCapturePrototype> _сaptureDictBySourceTag = [];

    private readonly List<(Func<EntityUid, MiGoCapturePrototype?> source, string sourceName)> _recipeSources = [];//ToDo_SS220 Remake this into 1 generated list "proto_source"->"proto_result"

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeCaptureRecipes();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<MiGoComponent, MiGoErectBuildMessage>(OnBuildMessage);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEraseMessage>(OnEraseMessage);
        SubscribeLocalEvent<MiGoComponent, MiGoErectCaptureMessage>(OnCaptureMessage);
        SubscribeLocalEvent<MiGoComponent, MiGoErectDoAfterEvent>(OnDoAfterErect);

        SubscribeLocalEvent<CultYoggBuildingFrameComponent, ComponentInit>(OnBuildingFrameInit);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, InteractUsingEvent>(OnBuildingFrameInteractUsing);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, GetVerbsEvent<InteractionVerb>>(AddInteractionVerbs);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, GetVerbsEvent<Verb>>(AddVerbs);
        SubscribeLocalEvent<CultYoggBuildingFrameComponent, ExaminedEvent>(OnBuildingFrameExamined);

        SubscribeLocalEvent<MiGoEraseDoAfterEvent>(OnEraseDoAfter);
        SubscribeLocalEvent<MiGoCaptureDoAfterEvent>(OnCaptureDoAfter);
    }

    public void OpenUI(Entity<MiGoComponent> entity, ActorComponent actor)
    {
        _userInterfaceSystem.TryToggleUi(entity.Owner, MiGoUiKey.Erect, actor.PlayerSession);
    }

    #region Building
    private void OnBuildMessage(Entity<MiGoComponent> entity, ref MiGoErectBuildMessage args)
    {
        if (entity.Owner != args.Actor)
            return;

        if (!_prototypeManager.TryIndex(args.BuildingId, out _))
            return;

        var erectAction = entity.Comp.MiGoErectActionEntity;

        if (erectAction == null || !TryComp<ActionComponent>(erectAction, out var actionComponent))
            return;

        if (actionComponent.Cooldown.HasValue && actionComponent.Cooldown.Value.End > _gameTiming.CurTime)
        {
            _popupSystem.PopupClient(Loc.GetString("cult-yogg-building-cooldown-popup"), entity, entity);
            return;
        }
        var location = GetCoordinates(args.Location);
        var tileRef = _turfSystem.GetTileRef(location);

        if (tileRef == null || _turfSystem.IsTileBlocked(tileRef.Value,
            Physics.CollisionGroup.MachineMask | Physics.CollisionGroup.Impassable | Physics.CollisionGroup.Opaque,
            minIntersectionArea: 0.15f))
        {
            _popupSystem.PopupClient(Loc.GetString("cult-yogg-building-tile-blocked-popup"), entity, entity);
            return;
        }

        if (!_interaction.InRangeUnobstructed(args.Actor, location, range: 2f, popup: true))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, entity, entity.Comp.ErectDoAfterSeconds,
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
        if (erectAction == null || !TryComp<ActionComponent>(erectAction, out var actionComponent))
            return;

        var cooldown = buildingPrototype.CooldownOverride ?? actionComponent.UseDelay ?? TimeSpan.FromSeconds(1);
        _actionsSystem.SetCooldown(erectAction, cooldown);
        args.Handled = true;
    }

    private void PlaceBuildingFrame(CultYoggBuildingPrototype buildingPrototype, EntityCoordinates location, Direction direction)
    {
        if (_netManager.IsClient)//no spawning on client
            return;

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

        return;
    }

    private EntityUid? PlaceCompleteBuilding(CultYoggBuildingPrototype buildingPrototype, EntityCoordinates location, Direction direction)
    {
        if (_netManager.IsClient)//no spawning on client
            return null;

        var building = SpawnAtPosition(buildingPrototype.ResultProtoId, location);
        Transform(building).LocalRotation = direction.ToAngle();

        return building;
    }
    #endregion

    #region Erase
    private void OnEraseMessage(Entity<MiGoComponent> entity, ref MiGoErectEraseMessage args)
    {
        if (entity.Owner != args.Actor)
            return;

        var buildingUid = EntityManager.GetEntity(args.BuildingFrame);
        if (_whitelistSystem.IsWhitelistFail(entity.Comp.EraseWhitelist, buildingUid))
        {
            _popupSystem.PopupClient(Loc.GetString("cult-yogg-building-cant-erase-non-cultists-buildings"), entity, entity);
            return;
        }

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

    private void OnEraseDoAfter(MiGoEraseDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is { } target)
            DeconstructBuilding(target);
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
    #endregion

    #region Capture
    private void OnCaptureMessage(Entity<MiGoComponent> ent, ref MiGoErectCaptureMessage args)
    {
        if (ent.Owner != args.Actor)
            return;

        var buildingUid = EntityManager.GetEntity(args.CapturedBuilding);

        if (HasComp<CultYoggBuildingComponent>(buildingUid))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-yogg-building-cant-capture-cult-building"), buildingUid, ent);
            return;
        }

        var prototypeId = MetaData(buildingUid).EntityPrototype;

        if (prototypeId == null)
            return;

        if (!TryGetCaptureRecipe(buildingUid, out var recipe))
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-yogg-building-cant-capture-this-building"), buildingUid, ent);
            return;
        }

        if (recipe == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("cult-yogg-building-cant-capture-this-building"), buildingUid, ent);
            return;
        }

        if (ent.Comp.CaptureCooldowns.TryGetValue(recipe.ReplacementProto.Id, out var cooldownTime) && cooldownTime > _gameTiming.CurTime)
        {
            _popupSystem.PopupClient(Loc.GetString("cult-yogg-building-caprure-cooldown", ("time", Math.Round((cooldownTime - _gameTiming.CurTime).TotalSeconds))), ent);
            return;
        }

        var e = new MiGoCaptureDoAfterEvent() { Recipe = recipe };
        var doafterArgs = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.CaptureDoAfterTime, e, ent.Owner, buildingUid)
        {
            Broadcast = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };
        _doAfterSystem.TryStartDoAfter(doafterArgs);
    }

    private void OnCaptureDoAfter(MiGoCaptureDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Recipe == null)
            return;


        if (args.Target is { } target)
        {
            var prototypeId = MetaData(target).EntityPrototype;

            if (prototypeId == null)
                return;

            StartReplacement(target, args.Recipe, prototypeId);
        }

        if (TryComp<MiGoComponent>(args.User, out var miGo))
            AddCaptureCooldownByResult((args.User, miGo), args.Recipe);//its wierd, but idk how to not make it with this event
    }

    private void StartReplacement(EntityUid buildingUid, MiGoCapturePrototype replacement, EntityPrototype buildingProto)
    {
        if (_gameTiming.InPrediction)
            return; // this should never run in client

        var xform = Transform(buildingUid);
        var rot = xform.LocalRotation;

        var newEntity = SpawnAtPosition(replacement.ReplacementProto, xform.Coordinates);
        Transform(newEntity).LocalRotation = rot;

        if (TryComp<ChameleonStructureComponent>(newEntity, out var structureChameleon))
        {
            _chameleonStructureSystem.SetPrototype((newEntity, structureChameleon), buildingProto.ID);//make it chameleon if possible
        }

        Del(buildingUid);
    }

    /// <summary>
    /// We need to re-initialize our recepies if prototypes are reloaded.
    /// </summary>
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<MiGoCapturePrototype>())
            return;

        InitializeCaptureRecipes();
    }

    /// <summary>
    /// Fills in the recipes dictionary from prototypes cache.
    /// </summary>
    private void InitializeCaptureRecipes()
    {
        _сaptureDictBySourcePrototypeId.Clear();
        _сaptureDictByParentPrototypeId.Clear();
        _сaptureDictBySourceTag.Clear();

        foreach (var recipe in _prototypeManager.EnumeratePrototypes<MiGoCapturePrototype>())
        {
            if (recipe.FromEntity.PrototypeId is { } prototypeId)
                _сaptureDictBySourcePrototypeId.Add(prototypeId, recipe);
            else if (recipe.FromEntity.ParentPrototypeId is { } parentPrototypeId)
                _сaptureDictByParentPrototypeId.Add(parentPrototypeId, recipe);
            else if (recipe.FromEntity.Tag is { } tag)
                _сaptureDictBySourceTag.Add(tag, recipe);
            else
                Log.Warning($"MiGoCapturePrototype with id '{recipe.ID}' has no ways to be used");
        }

        _recipeSources.Add((GetRecipeBySourcePrototypeId, "Prototype Id"));
        _recipeSources.Add((GetRecipeByParentPrototypeId, "Parent Prototype Id"));
        _recipeSources.Add((GetRecipeBySourceTag, "Tag"));
    }

    /// <summary>
    /// Returns recipe to capture specified entity, if any.
    /// </summary>
    /// <param name="uid">Entity to corrupt</param>
    /// <param name="capture">Result recipe</param>
    private bool TryGetCaptureRecipe(EntityUid uid, [NotNullWhen(true)] out MiGoCapturePrototype? capture)
    {
        capture = null;
        foreach (var (sourceFunc, sourceName) in _recipeSources)
        {
            capture = sourceFunc(uid);
            if (capture is null)
                continue;
            Log.Debug($"Founded capture recipe {capture.ID} for {ToPrettyString(uid)} via {sourceName}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Just use <see cref="TryGetCaptureRecipe(EntityUid, out MiGoCapturePrototype?)"/>
    /// </summary>
    private MiGoCapturePrototype? GetRecipeBySourcePrototypeId(EntityUid uid)
    {
        var prototypeId = MetaData(uid).EntityPrototype?.ID;
        if (prototypeId == null)
            return null;
        return _сaptureDictBySourcePrototypeId.GetValueOrDefault(prototypeId);
    }

    /// <summary>
    /// Just use <see cref="TryGetCaptureRecipe(EntityUid, out MiGoCapturePrototype?)"/>
    /// </summary>
    private MiGoCapturePrototype? GetRecipeByParentPrototypeId(EntityUid uid)
    {
        var parents = MetaData(uid).EntityPrototype?.Parents;
        if (parents == null)
            return null;

        foreach (var parentId in parents)
        {
            if (_сaptureDictByParentPrototypeId.TryGetValue(parentId, out var recipe))
                return recipe;

            var parentRecipe = GetRecipeByParentPrototypeId(parentId);
            if (parentRecipe != null)
                return parentRecipe;
        }
        return null;
    }
    /// <summary>
    /// Overload to see parents
    /// </summary>
    private MiGoCapturePrototype? GetRecipeByParentPrototypeId(string id)
    {
        if (!_prototypeManager.TryIndex<EntityPrototype>(id, out var entProto))
            return null;

        var parents = entProto.Parents;
        if (parents == null)
            return null;

        foreach (var parentId in parents)
        {
            if (_сaptureDictByParentPrototypeId.TryGetValue(parentId, out var recipe))
                return recipe;

            var parentRecipe = GetRecipeByParentPrototypeId(parentId);
            if (parentRecipe != null)
                return parentRecipe;
        }
        return null;
    }

    /// <summary>
    /// Just use <see cref="TryGetCaptureRecipe(EntityUid, out MiGoCapturePrototype?)"/>
    /// </summary>
    private MiGoCapturePrototype? GetRecipeBySourceTag(EntityUid uid)
    {
        if (!TryComp(uid, out TagComponent? tagComponent))
            return null;

        foreach (var tag in tagComponent.Tags)
        {
            if (_сaptureDictBySourceTag.TryGetValue(tag, out var recipe))
                return recipe;
        }
        return null;
    }

    private void AddCaptureCooldownByResult(Entity<MiGoComponent> ent, MiGoCapturePrototype recipe)
    {
        ent.Comp.CaptureCooldowns.TryAdd(recipe.ReplacementProto.Id, _gameTiming.CurTime + recipe.ReplacementCooldown);
    }
    #endregion

    #region FrameInteractions
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

                var locKey = addedCount >= neededMaterial.Count
                    ? "cult-yogg-building-frame-examined-material-full"
                    : "cult-yogg-building-frame-examined-material-needed";

                if (!_prototypeManager.TryIndex(neededMaterial.StackType, out var stackType))
                    continue;

                var materialName = Loc.GetString(stackType.Name);
                args.PushMarkup(Loc.GetString(locKey, ("material", materialName), ("currentAmount", addedCount), ("totalAmount", neededMaterial.Count)));
            }
        }
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

    private EntityUid? CompleteBuilding(Entity<CultYoggBuildingFrameComponent> entity)
    {
        if (_gameTiming.InPrediction) // this should never run in client
            return null;

        if (!_prototypeManager.TryIndex(entity.Comp.BuildingPrototypeId, out var prototype, logError: true))
            return null;

        var transform = Transform(entity);

        var resultEntity = PlaceCompleteBuilding(prototype, transform.Coordinates, transform.LocalRotation.GetDir());

        if (resultEntity == null)
            return null;

        Del(entity);

        return resultEntity;
    }
    #endregion
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

[Serializable, NetSerializable]
public sealed partial class MiGoCaptureDoAfterEvent : SimpleDoAfterEvent
{
    public MiGoCapturePrototype? Recipe;
}
