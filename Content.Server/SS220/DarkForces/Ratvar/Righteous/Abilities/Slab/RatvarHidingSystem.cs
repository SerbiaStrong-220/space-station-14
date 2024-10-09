using System.Linq;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Roles;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Slab;

public sealed class RatvarHidingSystem : EntitySystem
{
    //TODO придумать нормальный парсинг
    private static readonly string[] StructuresForReplace =
    {
        "PottedPlantAlt0",
        "PottedPlantAlt1",
        "PottedPlantAlt2",
        "PottedPlantAlt3",
        "PottedPlantAlt4",
        "PottedPlantAlt5",
        "PottedPlantAlt6",
        "PottedPlantAlt7",
        "PottedPlantAlt8",
        "ClosetTool",
        "ClosetRadiationSuit",
        "ClosetEmergency",
        "ClosetFire",
        "ClosetBomb",
        "ClosetL3",
        "ClosetMaintenance"
    };

    private static readonly string[] Toys =
    {
        "PlushieGhost",
        "PlushieBee",
        "PlushieHampter",
        "PlushieNuke",
        "PlushieRouny",
        "PlushieLamp",
        "PlushieLizard",
        "PlushieSpaceLizard",
        "PlushieDiona",
        "PlushieSharkBlue",
        "PlushieRatvar",
        "PlushieNar",
        "PlushieCarp",
        "PlushieSlime",
        "PlushieSnake",
        "ToyMouse",
        "ToyRubberDuck",
        "PlushieVox",
        "PlushieAtmosian",
        "PlushieXeno",
        "ToyIan",
        "ToyAmongPequeno",
        "PlushieMoth",
        "PlushiePenguin"
    };

    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _sharedTransform = default!;
    private EntityUid? PausedMap { get; set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RatvarHidingStructureComponent, ExaminedEvent>(OnExaminedStructureEvent);
        SubscribeLocalEvent<RatvarHidingItemComponent, ExaminedEvent>(OnExaminedItemEvent);
    }

    private void OnExaminedItemEvent(EntityUid uid, RatvarHidingItemComponent component, ExaminedEvent args)
    {
        if (!HasComp<RatvarRighteousComponent>(args.Examiner) || component.OriginalItem == null)
            return;

        var originalName = MetaData(component.OriginalItem.Value);

        args.PushMarkup("Предмет покрыт чарами");
        args.PushMarkup($"Оригинальный предмет: [color=#b87333]{originalName.EntityName}[/color]");
    }

    private void OnExaminedStructureEvent(EntityUid uid, RatvarHidingStructureComponent component, ExaminedEvent args)
    {
        if (!HasComp<RatvarRighteousComponent>(args.Examiner) || component.OriginalStructure == null)
            return;

        var originalName = MetaData(component.OriginalStructure.Value);

        args.PushMarkup("Структура покрыта чарами");
        args.PushMarkup($"Оригинальная структура: [color=#b87333]{originalName.EntityName}[/color]");
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        EntityManager.DeleteEntity(PausedMap.Value);
    }

    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        var newMap = _mapManager.CreateMap();
        PausedMap = _mapManager.GetMapEntityId(newMap);

        _metaData.SetEntityName(PausedMap.Value, "Карта Ратвара, не трогать!");
        _mapManager.SetMapPaused(newMap, true);
    }

    private void TeleportToMap(EntityUid target)
    {
        EnsurePausedMap();

        if (PausedMap == null)
            return;

        var transform = Transform(PausedMap.Value);

        _sharedTransform.SetCoordinates(target, transform.Coordinates);
        _sharedTransform.AttachToGridOrMap(target);
    }

    public bool HideStructure(EntityUid structure, EntityUid slab)
    {
        if (!HasComp<RatvarStructureComponent>(structure))
            return false;

        var structures = StructuresForReplace.ToList();
        _random.Shuffle(structures);
        var randomStructure = _random.Pick(structures);

        var hideStructureTransform = Transform(structure);
        var targetStructure = Spawn(randomStructure, hideStructureTransform.Coordinates);

        var hidingStructureComponent = EnsureComp<RatvarHidingStructureComponent>(targetStructure);
        hidingStructureComponent.OriginalStructure = structure;
        hidingStructureComponent.HidingSlab = slab;

        TeleportToMap(structure);
        return true;
    }

    public bool IsTargetHidingStructure(EntityUid target)
    {
        return HasComp<RatvarHidingStructureComponent>(target);
    }

    public bool BackStructure(EntityUid structure, out EntityUid hidingSlab)
    {
        hidingSlab = default;

        if (!TryComp<RatvarHidingStructureComponent>(structure, out var hiding) || hiding.OriginalStructure is not { Valid: true } target)
            return false;

        var transform = Transform(structure);
        _sharedTransform.SetCoordinates(target, transform.Coordinates);
        _sharedTransform.AttachToGridOrMap(target);
        _sharedTransform.AnchorEntity(target, Transform(target));

        QueueDel(structure);

        if (hiding.HidingSlab != null)
        {
            hidingSlab = hiding.HidingSlab.Value;
        }

        return true;
    }

    public bool HideItem(EntityUid user, EntityUid item)
    {
        if (!HasComp<RatvarSlabComponent>(item))
            return false;

        var toys = Toys.ToList();
        _random.Shuffle(toys);
        var randomToy = _random.Pick(toys);
        var hideStructureTransform = Transform(item);
        var targetToy = Spawn(randomToy, hideStructureTransform.Coordinates);

        var hidingStructureComponent = EnsureComp<RatvarHidingItemComponent>(targetToy);
        hidingStructureComponent.OriginalItem = item;

        TeleportToMap(item);
        _hands.TryPickupAnyHand(user, targetToy);
        return true;
    }

    public bool BackItem(EntityUid user, EntityUid item)
    {
        if (!TryComp<RatvarHidingItemComponent>(item, out var hiding) ||
            hiding.OriginalItem is not { Valid: true } target)
            return false;

        var transform = Transform(item);
        _sharedTransform.SetCoordinates(target, transform.Coordinates);
        _sharedTransform.AttachToGridOrMap(target);

        _hands.TryDrop(user, item);
        _hands.TryPickupAnyHand(user, target);

        QueueDel(item);

        return true;
    }
}
