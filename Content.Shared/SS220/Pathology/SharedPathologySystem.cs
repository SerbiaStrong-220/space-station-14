// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public static readonly int OneStack = 1;

    public static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);

    private TimeSpan _lastUpdate;

    public override void Initialize()
    {
        SubscribeLocalEvent<PathologyHolderComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTiming.Paused)
            return;

        if (_gameTiming.CurTime < _lastUpdate)
            return;

        _lastUpdate = _gameTiming.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.ActivePathologies.Count == 0)
                continue;

            foreach (var (protoId, data) in holder.ActivePathologies)
            {
                if (!_prototype.Resolve(protoId, out var pathologyProto))
                    continue;

                TryProgressPathology((uid, holder), pathologyProto, data, null);

                foreach (var effect in pathologyProto.Definition[data.Level].Effects)
                {
                    effect.ApplyEffect(uid, data, EntityManager);
                }
            }
        }
    }

    private void OnRejuvenate(Entity<PathologyHolderComponent> entity, ref RejuvenateEvent args)
    {
        foreach (var (pathologyId, _) in entity.Comp.ActivePathologies)
        {
            TryRemovePathology(entity!, pathologyId, checkStacks: false);
        }
    }

    private bool TryProgressPathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, PathologyInstanceData instanceData, IPathologyContext? context)
    {
        if (pathologyPrototype.Definition[instanceData.Level].ProgressConditions.Length == 0)
            return false;

        foreach (var req in pathologyPrototype.Definition[instanceData.Level].ProgressConditions)
        {
            if (req.CheckCondition(entity, instanceData, EntityManager))
                continue;

            return false;
        }

        if (instanceData.Level + 1 >= pathologyPrototype.Definition.Length)
            return false;

        instanceData.Level++;
        // we drop all previous
        instanceData.StackCount = OneStack;
        instanceData.PathologyContexts.Clear();

        instanceData.PathologyContexts.Add(context);
        AddPathologyDefinitionEffects(entity, pathologyPrototype.Definition[instanceData.Level]);

        var ev = new PathologySeverityChanged(pathologyPrototype.ID, instanceData.Level - 1, instanceData.Level);
        RaiseLocalEvent(entity, ref ev);

        Dirty(entity);
        return true;
    }

    private void StartPathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, IPathologyContext? context)
    {
        var ev = new PathologyAddedEvent(pathologyPrototype.ID);
        RaiseLocalEvent(entity, ref ev);

        AddPathologyDefinitionEffects(entity, pathologyPrototype.Definition[0]);

        entity.Comp.ActivePathologies.Add(pathologyPrototype.ID, new PathologyInstanceData(_gameTiming.CurTime, context));
        Dirty(entity);
    }

    private void AddPathologyDefinitionEffects(Entity<PathologyHolderComponent> entity, PathologyDefinition definition)
    {
        foreach (var effect in definition.StatusEffects)
        {
            _statusEffects.TrySetStatusEffectDuration(entity, effect, out _);
        }

        AddTrait(entity, definition.Trait);

        // we don't want to popup dead ones
        if (_mobState.IsIncapacitated(entity))
            return;

        if (definition.ProgressPopup is { } progressPopup)
            _popup.PopupEntity(Loc.GetString(progressPopup), entity, entity, PopupType.MediumCaution);
    }

    private void RemovePathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype)
    {
        var data = entity.Comp.ActivePathologies[pathologyPrototype.ID];

        for (var i = 0; i < data.Level; i++)
        {
            if (i >= pathologyPrototype.Definition.Length)
            {
                Log.Error($"Got level more than pathology definitions in {pathologyPrototype.ID} pathology!");
                break;
            }

            foreach (var effect in pathologyPrototype.Definition[i].StatusEffects)
            {
                _statusEffects.TryRemoveStatusEffect(entity, effect);
            }

            RemoveTrait(entity, pathologyPrototype.Definition[i].Trait);
        }

        foreach (var context in data.PathologyContexts)
        {
            ApplyPathologyContext(entity, context);
        }

        entity.Comp.ActivePathologies.Remove(pathologyPrototype.ID);

        Dirty(entity);
    }

    protected virtual void ApplyPathologyContext(Entity<PathologyHolderComponent> entity, IPathologyContext? context) { }

    // Kill it with TraitPrototype pls
    private void AddTrait(EntityUid uid, ProtoId<TraitPrototype>? traitId)
    {
        if (!_prototype.Resolve(traitId, out var traitPrototype))
            return;

        if (traitPrototype.Components is null)
            return;

        EntityManager.AddComponents(uid, traitPrototype.Components, false);
    }

    // and it
    private void RemoveTrait(EntityUid uid, ProtoId<TraitPrototype>? traitId)
    {
        if (!_prototype.Resolve(traitId, out var traitPrototype))
            return;

        if (traitPrototype.Components is null)
            return;

        EntityManager.RemoveComponents(uid, traitPrototype.Components);
    }
}
