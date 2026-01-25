using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Content.Shared.SS220.Deafenation;

public sealed class DeafenationSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private HashSet<EntityUid> _tempEntitySet = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NoiseSuppressionComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<InventoryComponent, OnDeafenedEvent>(
            OnInventoryDeafened);
    }

    public void DeafenArea(EntityUid source, EntityUid? user, float range, float knockdownTime, float stunTime, float probability = 1f)
    {
        var transform = Transform(source);
        var mapPosition = _transform.GetMapCoordinates(transform);

        _tempEntitySet.Clear();
        _entityLookup.GetEntitiesInRange(transform.Coordinates, range, _tempEntitySet);

        var rand = new System.Random((int)_timing.CurTick.Value + GetNetEntity(source).Id);

        foreach (var entity in _tempEntitySet)
        {
            if (!rand.Prob(probability))
                continue;

            if (!_examine.InRangeUnOccluded(entity, mapPosition, range, predicate: e => e == entity))
                continue;

            var distance = (_transform.GetMapCoordinates(entity).Position - mapPosition.Position).Length();

            ProcessDeafenTarget(entity, distance, range, knockdownTime, stunTime);
        }
    }

    private void ProcessDeafenTarget(EntityUid target, float distance, float range, float knockdownTime, float stunTime)
    {
        if (knockdownTime <= 0 && stunTime <= 0)
            return;

        var ev = new OnDeafenedEvent(range);
        RaiseLocalEvent(target, ev, true);

        var suppressionRange = ev.SuppressionRange;
        var deafeningRange = MathF.Max(0f, distance);

        if (deafeningRange > suppressionRange)
            return;

        var ratio = deafeningRange / suppressionRange;
        var effectiveKnockdown = float.Lerp(knockdownTime, 0f, ratio);
        var effectiveStun = float.Lerp(stunTime, 0f, ratio);

        if (effectiveKnockdown > 0f)
            _stunSystem.TryKnockdown(target, TimeSpan.FromSeconds(effectiveKnockdown));

        if (effectiveStun > 0f)
            _stunSystem.TryUpdateStunDuration(target, TimeSpan.FromSeconds(effectiveStun));
    }

    private void OnInventoryDeafened(Entity<InventoryComponent> ent, ref OnDeafenedEvent args)
    {
        foreach (var slot in new[] { "head", "ears" })
        {
            if (_inventory.TryGetSlotEntity(ent, slot, out var item, ent.Comp) &&
                TryComp<NoiseSuppressionComponent>(item, out var noiseSuppressor))
            {
                args.SuppressionRange = MathF.Min(args.SuppressionRange, noiseSuppressor.SuppressionRange);
            }
        }
    }

    private void OnExamined(Entity<NoiseSuppressionComponent> ent, ref ExaminedEvent args)
    {
        var message = ent.Comp.SuppressionRange > 0
            ? Loc.GetString("sound-suppression-examine", ("range", ent.Comp.SuppressionRange))
            : Loc.GetString("sound-suppression-fully-examine");

        args.PushMarkup(message);
    }
}
