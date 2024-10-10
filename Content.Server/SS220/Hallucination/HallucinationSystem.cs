// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Hallucination;
using Robust.Shared.Timing;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Inventory;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;

namespace Content.Server.SS220.Hallucination;
/// <summary>
/// System which make it easier to work with Hallucinations
/// </summary>
public sealed class HallucinationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public const float DelayBetweenHallucinateAttempt = 2f;

    /// <summary>
    /// Used to store protectComponents, see <see cref="HallucinationSystem.TryGetComponentType" />
    /// </summary>
    private SortedDictionary<string, Type?> _cachedProtectComponentsType = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HallucinationComponent, ComponentGetState>(GetComponentState);
        SubscribeLocalEvent<GotEquippedEvent>(OnEquip);

    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var hallucinationQuery = EntityQueryEnumerator<HallucinationComponent>();
        while (hallucinationQuery.MoveNext(out var entityUid, out var hallucination))
        {
            for (var i = 0; i < hallucination.TotalDurationTimeSpans.Count; i++)
            {
                if (_gameTiming.CurTime > hallucination.TotalDurationTimeSpans[i])
                    Remove((entityUid, hallucination), i);
            }
        }

        var sourceQuery = EntityQueryEnumerator<HallucinationSourceComponent>();
        while (sourceQuery.MoveNext(out var sourceUid, out var hallucinationSource))
        {
            if (_gameTiming.CurTime < hallucinationSource.NextUpdateTime)
                continue;

            hallucinationSource.NextUpdateTime += TimeSpan.FromSeconds(DelayBetweenHallucinateAttempt);
            if (!HasComp<TransformComponent>(sourceUid))
                continue;

            var hallucinationTargets = _entityLookup.GetEntitiesInRange<MindContainerComponent>(
                                        Transform(sourceUid).Coordinates, hallucinationSource.RangeOfHallucinations);
            foreach (var entity in hallucinationTargets)
                TryAdd(entity.Owner, hallucinationSource.Hallucination);
        }
    }
    /// <summary>
    /// Check if entity is protected from hallucination and if not.
    /// After that checks if hallucination exist and than renews its timer.
    /// Adds component if needed and then after adding hallucination dirties.
    /// </summary>
    /// <returns> false if protected and true if not</returns>
    public bool TryAdd(EntityUid target, HallucinationSetting hallucination)
    {
        if (Protected(target, hallucination))
            return false;

        if (TryFindHallucination(target, hallucination))
        {
            var comp = Comp<HallucinationComponent>(target);
            var index = comp.Hallucinations.IndexOf(hallucination);
            comp.TotalDurationTimeSpans[index] = _gameTiming.CurTime + TimeSpan.FromSeconds(hallucination.TotalDuration);
            return true;
        }

        Add(target, hallucination);
        return true;
    }

    /// <summary>
    /// False if target dont have HallucinationComponent or hallucination doesnt exists otherwise true.
    /// </summary>
    public bool Remove(Entity<HallucinationComponent> target, int index)
    {
        var (_, comp) = target;

        if (comp.Deleted)
            return false;

        if (comp.Hallucinations.Count <= index
            && comp.TotalDurationTimeSpans.Count <= index)
        {
            Log.Error("Tried to Remove Hallucination with index more than Count one of the Lists");
            return false;
        }

        comp.Hallucinations.RemoveAt(index);
        comp.TotalDurationTimeSpans.RemoveAt(index);
        if (comp.Hallucinations.Count != comp.TotalDurationTimeSpans.Count)
            Log.Error("After removing of the hallucination Counts of both list doesnt match.");

        Dirty(target);
        return true;
    }
    /// <inheritdoc cref="HallucinationSystem.Remove" />
    public bool Remove(Entity<HallucinationComponent?> target, HallucinationSetting hallucination)
    {
        if (!Resolve(target.Owner, ref target.Comp))
            return false;

        var index = target.Comp.Hallucinations.IndexOf(hallucination);
        return Remove((target.Owner, target.Comp), index);
    }
    /// <inheritdoc cref="HallucinationSystem.Remove" />
    public bool Remove(Entity<HallucinationComponent?> target, TimeSpan time)
    {
        if (!Resolve(target.Owner, ref target.Comp))
            return false;

        var index = target.Comp.TotalDurationTimeSpans.IndexOf(time);
        return Remove((target.Owner, target.Comp), index);
    }

    /// <summary>
    /// False if target dont have HallucinationComponent or if key wasnt found
    /// </summary>
    public bool TryFindHallucination(EntityUid target, HallucinationSetting hallucination)
    {
        if (!TryComp<HallucinationComponent>(target, out var hallucinationComponent))
            return false;

        return hallucinationComponent.Hallucinations.TryFirstOrNull(hallucination.Equals, out _);
    }

    private void Add(EntityUid target, HallucinationSetting hallucination)
    {
        var hallucinationComponent = EnsureComp<HallucinationComponent>(target);

        Add((target, hallucinationComponent), hallucination);
    }
    private void Add(Entity<HallucinationComponent> target, HallucinationSetting hallucination)
    {
        var (_, comp) = target;

        if (comp.Deleted)
            return;

        comp.Hallucinations.Add(hallucination);

        if (hallucination.TimeParams.TotalDuration == float.NaN)
            comp.TotalDurationTimeSpans.Add(null);
        else
            comp.TotalDurationTimeSpans.Add(_gameTiming.CurTime
                                                        + TimeSpan.FromSeconds(hallucination.TimeParams.TotalDuration));

        if (comp.Hallucinations.Count != comp.TotalDurationTimeSpans.Count)
            Log.Error("After adding of the hallucination Counts of both list doesnt match.");

        Dirty(target);
    }
    /// <summary>
    /// Think of sending only to PlayersEntity
    /// </summary>
    private void GetComponentState(Entity<HallucinationComponent> entity, ref ComponentGetState args)
    {
        args.State = new HallucinationComponentState
        {
            Hallucinations = entity.Comp.Hallucinations
        };
    }
    /// <summary>
    /// Here we proceed equipping clothing with protection
    /// </summary>
    /// <param name="args"></param>
    private void OnEquip(GotEquippedEvent args)
    {
        if (TryComp<HallucinationComponent>(args.Equipee, out var hallucinationComponent))
        {
            foreach (var hallucination in new List<HallucinationSetting>(hallucinationComponent.Hallucinations))
            {
                if (hallucination.Protection.ComponentName == null)
                    continue;

                if (!TryGetComponentType(hallucination.Protection.ComponentName, out var protectionComponentType))
                    continue;

                if (ItemProtects(args.Equipment, args.SlotFlags, protectionComponentType, hallucination.Protection.CheckPockets))
                    Remove(args.Equipee, hallucination);
            }
        }
    }
    /// <summary>
    /// Checks if Entity is protected by anything like components on Entity or equipment on it/him/her/etc
    /// </summary>
    private bool Protected(EntityUid mobUid, HallucinationSetting hallucination)
    {
        if (HasComp<HallucinationImmuneComponent>(mobUid))
            return true;

        var protection = hallucination.Protection;
        if (protection.ComponentName == null)
            return false;

        if (!TryGetComponentType(protection.ComponentName, out var protectionComponentType))
            return false;

        var inventorySlot = protection.ItemSlot.HasValue ?
                        _inventory.GetSlotEnumerator(mobUid, protection.ItemSlot.Value) :
                        _inventory.GetSlotEnumerator(mobUid);

        while (inventorySlot.NextItem(out var itemUid, out var slot))
        {
            if (ItemProtects(itemUid, slot.SlotFlags, protectionComponentType, protection.CheckPockets))
                return true;
        }

        return false;
    }
    /// <summary>
    /// Use to check if item is able to protect from hallucination
    /// </summary>
    private bool ItemProtects(EntityUid itemUid, SlotFlags slotFlag, Type protectionComponent, bool checkPockets)
    {
        if (!checkPockets
            && slotFlag == SlotFlags.POCKET)
            return false;

        if (HasComp(itemUid, protectionComponent))
            return true;

        return false;
    }
    private bool TryGetComponentType(string componentName, [NotNullWhen(true)] out Type? componentType)
    {
        if (_cachedProtectComponentsType.TryGetValue(componentName, out componentType))
            return componentType != null;

        if (!_componentFactory.TryGetRegistration(componentName, out var componentRegistration))
        {
            Log.Error($"Tried to get registration of component {componentName}");
            return false;
        }

        componentType = _componentFactory.GetComponent(componentRegistration).GetType();
        _cachedProtectComponentsType.Add(componentName, componentType);
        return true;
    }
}
