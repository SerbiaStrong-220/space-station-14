// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.SoftDelete;
using Robust.Shared.Serialization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

/// <summary>
/// Handles all corruption logic.
/// </summary>
public sealed class SharedCultYoggCorruptedSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSoftDeleteSystem _softDeleteSystem = default!;

    private readonly TimeSpan _corruptionDuration = TimeSpan.FromSeconds(3);
    private readonly Dictionary<ProtoId<EntityPrototype>, CultYoggCorruptedPrototype> _recipesBySourcePrototypeId = [];
    private readonly Dictionary<ProtoId<StackPrototype>, CultYoggCorruptedPrototype> _recipesBySourceStackType = [];
    private readonly Dictionary<ProtoId<EntityPrototype>, CultYoggCorruptedPrototype> _recipiesByParentPrototypeId = [];
    private readonly Dictionary<ProtoId<TagPrototype>, CultYoggCorruptedPrototype> _recipiesBySourceTag = [];
    private readonly List<EntityUid> _dropEntitiesBuffer = [];

    private readonly List<(Func<EntityUid, CultYoggCorruptedPrototype?> source, string sourceName)> _recipeSources = new();

    public override void Initialize()
    {
        InitializeRecipes();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptDoAfterEvent>(OnDoAfterCorruption);

        _recipeSources.Add((GetRecipeBySourcePrototypeId, "Prototype Id"));
        _recipeSources.Add((GetRecipeBySourceStackType, "Stack Type"));
        _recipeSources.Add((GetRecipeByParentPrototypeId, "Parent Prototype Id"));
        _recipeSources.Add((GetRecipeBySourceTag, "Tag"));
    }

    /// <summary>
    /// Checks if specified entity has corrupted counterpart.
    /// </summary>
    /// <param name="entity">Entiry to corrupt</param>
    /// <returns><see langword="true"/> if way to corrupt specified entity exists, otherwise <see langword="false"/></returns>
    public bool HasCorruptionRecipe(EntityUid entity)
    {
        return TryGetCorruptionRecipe(entity, out _);
    }

    /// <summary>
    /// Checks if specified entity is already corrupted.
    /// </summary>
    /// <param name="entity">Target entity</param>
    /// <returns><see langword="true"/> if entity is corrupted, otherwise <see langword="false"/></returns>
    public bool IsCorrupted(EntityUid entity)
    {
        return _entityManager.HasComponent<CultYoggCorruptedComponent>(entity);
    }

    /// <summary>
    /// Reverts corruption of the corrupted entity. Note that corrupted entity will be deleted and the new original one returned.
    /// </summary>
    /// <param name="corruptedEntity">Corrupted entity to revert</param>
    /// <returns>Newly created original form of the corrupted entity if any, otherwise <see langword="null"/></returns>
    public EntityUid? RevertCorruption(Entity<CultYoggCorruptedComponent> corruptedEntity, out CultYoggCorruptedPrototype? recipe)
    {
        recipe = GetRecipeById(corruptedEntity.Comp.Recipe);

        if (recipe is null)
            return null;

        _containerSystem.TryRemoveFromContainer(corruptedEntity, force: true);

        var coords = Transform(corruptedEntity).Coordinates;
        EntityUid normalEntity;
        if (corruptedEntity.Comp.SoftDeletedOriginalEntity is { } originalEntity)
        {
            _softDeleteSystem.TryRestore(originalEntity);
            _transformSystem.SetCoordinates(originalEntity, coords);
            normalEntity = originalEntity;
        }
        else if (corruptedEntity.Comp.OriginalPrototypeId is { } originalPrototypeId)
        {
            normalEntity = Spawn(originalPrototypeId, coords);
            TryTransformStack(recipe, normalEntity, corruptedEntity, normalEntity);
        }
        else
        {
            return null;
        }

        TryDropAllContainedEntities(corruptedEntity);
        _entityManager.DeleteEntity(corruptedEntity);

        return normalEntity;
    }

    /// <summary>
    /// Immediately corrupts specified entity, if possible. Note that original entity will be deleted and the new corrupted one returned.
    /// </summary>
    /// <param name="user">Entity that performs a corruption</param>
    /// <param name="entity">Entity to corrupt</param>
    /// <param name="isInHand">Flag indicating that the target entity is in hand</param>
    /// <returns>Newly created corrupted entity if corruption is possible, otherwise <see langword="null"/></returns>
    public EntityUid? TryCorruptImmediately(EntityUid user, EntityUid entity, bool isInHand)
    {
        if (!TryGetCorruptionRecipe(entity, out var recipe))
        {
            return null;
        }
        return Corrupt(user, entity, recipe, isInHand);
    }

    /// <summary>
    /// Starts a continuous corruption process over specified entity. Note that original entity will be deleted and the new corrupted one returned.
    /// </summary>
    /// <param name="user">Entity that performs a corruption</param>
    /// <param name="entity">Entity to corrupt</param>
    /// <param name="isInHand">Flag indicating that the target entity is in hand</param>
    /// <param name="callback">Optional callback to fire when finished</param>
    /// <returns><see langword="true"/> if corruption process has been started, otherwise <see langword="false"/></returns>
    public bool TryCorruptContinuously(EntityUid user, EntityUid entity, bool isInHand, Action<EntityUid?>? callback = null)
    {
        if (!TryGetCorruptionRecipe(entity, out var recipe))
        {
            return false;
        }
        var e = new CultYoggCorruptDoAfterEvent(recipe, isInHand, callback);
        var doafterArgs = new DoAfterArgs(EntityManager, user, _corruptionDuration, e, user, entity) //ToDo estimate time for corruption
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };
        _doAfter.TryStartDoAfter(doafterArgs);
        return true;
    }

    /// <summary>
    /// We need to re-initialize our recepies if prototypes are reloaded.
    /// </summary>
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<CultYoggCorruptedPrototype>())
            return;
        InitializeRecipes();
    }

    /// <summary>
    /// Continuous corruption after event handler.
    /// </summary>
    private void OnDoAfterCorruption(EntityUid uid, CultYoggComponent component, CultYoggCorruptDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (_net.IsClient)
            return;

        if (args.Proto == null)
            return;

        var corrupted = Corrupt(uid, args.Target.Value, args.Proto, args.InHand);
        args.Callback?.Invoke(corrupted);

        args.Handled = true;
    }

    /// <summary>
    /// Returns recipe to corrupt specified entity, if any.
    /// </summary>
    /// <param name="uid">Entity to corrupt</param>
    /// <param name="corruption">Result recipe</param>
    private bool TryGetCorruptionRecipe(EntityUid uid, [NotNullWhen(true)] out CultYoggCorruptedPrototype? corruption)
    {
        corruption = null;
        foreach (var (sourceFunc, sourceName) in _recipeSources)
        {
            corruption = sourceFunc(uid);
            if (corruption is null)
                continue;
            Log.Debug("Founded corruption recipe {0} for {1} via {2}", corruption.ID, ToPrettyString(uid), sourceName);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Just use <see cref="TryGetCorruptionRecipe(EntityUid, out CultYoggCorruptedPrototype?)"/>
    /// </summary>
    private CultYoggCorruptedPrototype? GetRecipeBySourcePrototypeId(EntityUid uid)
    {
        var prototypeId = MetaData(uid).EntityPrototype?.ID;
        if (prototypeId == null)
            return null;
        return _recipesBySourcePrototypeId.GetValueOrDefault(prototypeId);
    }

    /// <summary>
    /// Just use <see cref="TryGetCorruptionRecipe(EntityUid, out CultYoggCorruptedPrototype?)"/>
    /// </summary>
    private CultYoggCorruptedPrototype? GetRecipeBySourceStackType(EntityUid uid)
    {
        if (!TryComp(uid, out StackComponent? stack))
            return null;
        return _recipesBySourceStackType.GetValueOrDefault(stack.StackTypeId);
    }

    /// <summary>
    /// Just use <see cref="TryGetCorruptionRecipe(EntityUid, out CultYoggCorruptedPrototype?)"/>
    /// </summary>
    private CultYoggCorruptedPrototype? GetRecipeByParentPrototypeId(EntityUid uid)
    {
        var parents = MetaData(uid).EntityPrototype?.Parents;
        if (parents == null)
            return null;
        foreach (var parentId in parents)
        {
            if (_recipiesByParentPrototypeId.TryGetValue(parentId, out var recipe))
                return recipe;
        }
        return null;
    }

    /// <summary>
    /// Just use <see cref="TryGetCorruptionRecipe(EntityUid, out CultYoggCorruptedPrototype?)"/>
    /// </summary>
    private CultYoggCorruptedPrototype? GetRecipeBySourceTag(EntityUid uid)
    {
        if (!TryComp(uid, out TagComponent? tagComponent))
            return null;
        foreach (var tag in tagComponent.Tags)
        {
            if (_recipiesBySourceTag.TryGetValue(tag, out var recipe))
                return recipe;
        }
        return null;
    }

    private CultYoggCorruptedPrototype? GetRecipeById(ProtoId<CultYoggCorruptedPrototype>? id)
    {
        if (!id.HasValue)
            return null;
        return _prototypeManager.Index(id.Value);
    }

    /// <summary>
    /// Fills in the recipes dictionary from prototypes cache.
    /// </summary>
    private void InitializeRecipes()
    {
        _recipesBySourcePrototypeId.Clear();
        _recipesBySourceStackType.Clear();
        _recipiesByParentPrototypeId.Clear();
        _recipiesBySourceTag.Clear();
        foreach (var recipe in _prototypeManager.EnumeratePrototypes<CultYoggCorruptedPrototype>())
        {
            if (recipe.FromEntity.PrototypeId is { } prototypeId)
                _recipesBySourcePrototypeId.Add(prototypeId, recipe);
            else if (recipe.FromEntity.StackType is { } stackType)
                _recipesBySourceStackType.Add(stackType, recipe);
            else if (recipe.FromEntity.ParentPrototypeId is { } parentPrototypeId)
                _recipiesByParentPrototypeId.Add(parentPrototypeId, recipe);
            else if (recipe.FromEntity.Tag is { } tag)
                _recipiesBySourceTag.Add(tag, recipe);
            else
                Log.Warning("CultYoggCorruptedPrototype with id '{0}' has no ways to be used", recipe.ID);
        }
    }

    /// <summary>
    /// Basically replaces entity with its corrupted counterpart according to recipe.
    /// </summary>
    /// <param name="user">Entity that performs a corruption</param>
    /// <param name="entity">Entity to corrupt</param>
    /// <param name="recipe">Recipe prototype</param>
    /// <param name="isInHand">Flag indicating that the target entity is in hand</param>
    /// <returns>Corrupted entity</returns>
    private EntityUid? Corrupt(EntityUid user, EntityUid entity, CultYoggCorruptedPrototype recipe, bool isInHand)
    {
        var coords = Transform(entity).Coordinates;
        var corruptedEntity = Spawn(recipe.Result, coords);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user)} used corrupt on {ToPrettyString(entity)} and made {ToPrettyString(corruptedEntity)}");

        if (isInHand)
            _hands.TryDrop(user, entity);

        if (recipe.EmptyStorage)
            TryDropAllContainedEntities(entity);

        EnsureComp<CultYoggCorruptedComponent>(corruptedEntity, out var corrupted);

        corrupted.SoftDeletedOriginalEntity = entity;
        corrupted.Recipe = recipe.ID;

        TryTransformStack(recipe, entity, entity, corruptedEntity);

        _softDeleteSystem.SoftDelete(entity);

        if (isInHand)
            _hands.PickupOrDrop(user, corruptedEntity);

        return corruptedEntity;
    }

    /// <summary>
    /// Checks and applies changes to <paramref name="entityTo"/> in case of stack curruption. Can be also usen to reverse corruption.
    /// </summary>
    /// <param name="recipe">Recipe to currupt/uncorrupt entity from</param>
    /// <param name="originalEntity">Entity that is considered as an original form of an object</param>
    /// <param name="entityFrom">Entity to get stack info from</param>
    /// <param name="entityTo">Entity to set stack info to</param>
    private bool TryTransformStack(CultYoggCorruptedPrototype recipe, EntityUid originalEntity, EntityUid entityFrom, EntityUid entityTo)
    {
        if (TryComp(entityFrom, out StackComponent? stackFrom) &&
            TryComp(entityTo, out StackComponent? stackTo) &&
            recipe.FromEntity.StackType == (originalEntity == entityFrom ? stackFrom : stackTo).StackTypeId)
        {
            _stackSystem.SetCount(entityTo, stackFrom.Count, stackTo);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Drops entities from all attached containers
    /// </summary>
    private bool TryDropAllContainedEntities(EntityUid entity)
    {
        if (!TryComp<ContainerManagerComponent>(entity, out var containerManager))
            return false;

        _dropEntitiesBuffer.Clear();
        var coords = Transform(entity).Coordinates;
        foreach (var container in _containerSystem.GetAllContainers(entity, containerManager))
        {
            foreach (var item in container.ContainedEntities)
            {
                _dropEntitiesBuffer.Add(item);
            }
        }
        foreach (var item in _dropEntitiesBuffer)
        {
            _transformSystem.AttachToGridOrMap(item);
            _transformSystem.SetCoordinates(item, coords);
            _transformSystem.SetWorldRotation(item, _random.NextAngle());
        }
        _dropEntitiesBuffer.Clear();
        return true;
    }
}

/// <summary>
/// Event raised after completeon of DoAfter
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CultYoggCorruptDoAfterEvent : SimpleDoAfterEvent
{
    public readonly bool InHand;
    public readonly CultYoggCorruptedPrototype? Proto;
    [NonSerialized]
    public readonly Action<EntityUid?>? Callback;

    public CultYoggCorruptDoAfterEvent(CultYoggCorruptedPrototype? proto, bool inHand, Action<EntityUid?>? callback)
    {
        InHand = inHand;
        Proto = proto;
        Callback = callback;
    }
}
