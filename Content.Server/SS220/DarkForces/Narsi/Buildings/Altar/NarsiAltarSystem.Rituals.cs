using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Prototypes;
using Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Rituals;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar;

public sealed partial class NarsiAltarSystem
{
    private void InitializeRituals()
    {
        SubscribeLocalEvent<NarsiAltarComponent, NarsiAltarStartRitualEvent>(OnRitualStartEvent);
        SubscribeLocalEvent<NarsiAltarComponent, NarsiRitualDoAftertEvent>(OnRitualEnded);
        SubscribeLocalEvent<NarsiAltarComponent, NarsiAltarOpenRituals>(OnOpenRituals);

        SubscribeLocalEvent<NarsiAltarComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<NarsiAltarComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnOpenRituals(EntityUid uid, NarsiAltarComponent component, NarsiAltarOpenRituals args)
    {
        if (!_ui.TryOpenUi(uid, NarsiRitualsInterfaceKey.Key, args.Actor))
            return;

        UpdateRitualsState(uid, component);
    }

    private void OnStrapped(Entity<NarsiAltarComponent> ent, ref StrappedEvent args)
    {
        if (!args.Buckle.Comp.Buckled)
            return;

        ent.Comp.BuckledEntity = args.Buckle.Owner;

        if (ent.Comp.ActualRitual == null)
            return;

        CheckRitual(ent);
    }

    private void OnUnstrapped(Entity<NarsiAltarComponent> ent, ref UnstrappedEvent args)
    {
        ent.Comp.BuckledEntity = null;
        if (ent.Comp.ActiveSound == null)
            return;

        CheckRitual(ent);
    }

    private void OnRitualEnded(EntityUid uid, NarsiAltarComponent component, NarsiRitualDoAftertEvent args)
    {
        if (args.Handled)
            return;


        var actualRitual = component.ActualRitual;
        if (args.Cancelled || actualRitual == null)
        {
            component.ActualRitual = null;
            component.DoAfterId = null;
            component.State = NarsiRitualsProgressState.Idle;

            UpdateRitualsState(uid, component);
            args.Handled = true;
            return;
        }

        actualRitual.Effect.MakeRitualEffect(uid, args.User, component, EntityManager);
        actualRitual.Effect.OnRitualFinished((uid, component), actualRitual, EntityManager);

        var ev = new NarsiRitualCompletedEvent(actualRitual.ID);
        RaiseLocalEvent(ref ev);
        CleanUpAltarComponent(uid, component);

        args.Handled = true;
    }

    private void CleanUpAltarComponent(EntityUid uid, NarsiAltarComponent component)
    {
        component.ActualRitual = null;
        component.DoAfterId = null;
        component.State = NarsiRitualsProgressState.Delay;
        component.RitualTimeoutTick = _timing.CurTime + component.RitualsThreshold;

        UpdateRitualsState(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        UpdateAltars(frameTime, curTime);
    }

    private void UpdateAltars(float frameTime, TimeSpan curTime)
    {
        var altars = EntityQueryEnumerator<NarsiAltarComponent>();
        while (altars.MoveNext(out var uid, out var component))
        {
            if (component.State == NarsiRitualsProgressState.Delay && component.RitualTimeoutTick <= curTime)
            {
                component.State = NarsiRitualsProgressState.Idle;
                UpdateRitualsState(uid, component);
            }

            if (component.ActualRitual is not { } ritual)
                continue;

            if (CheckRitual((uid, component)))
                component.ActualRitual?.Effect.OnUpdate((uid, component), ritual, EntityManager);
        }
    }

    private bool CheckRitual(Entity<NarsiAltarComponent> altar)
    {
        var ritual = altar.Comp.ActualRitual;
        if (ritual?.Effect.IsRitualAvailable(altar, ritual.Requirements, EntityManager) == true)
            return true;

        _doAfter.Cancel(altar.Comp.DoAfterId);
        ritual?.Effect.OnRitualCanceled(altar, ritual, EntityManager);

        return false;
    }

    private void OnRitualStartEvent(EntityUid uid, NarsiAltarComponent component, NarsiAltarStartRitualEvent args)
    {
        var ritual = _prototype.Index<NarsiRitualPrototype>(args.PrototypeId);
        if (!ritual.Effect.IsRitualAvailable((uid, component), ritual.Requirements, EntityManager))
            return;

        var doAfterEvent = new NarsiRitualDoAftertEvent();
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.Actor,
            TimeSpan.FromSeconds(ritual.Duration),
            doAfterEvent,
            uid,
            null,
            uid
        )
        {
            BreakOnMove = true,
            MovementThreshold = 5.0f
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs, out var doAfterId))
            return;

        component.ActualRitual = ritual;
        component.DoAfterId = doAfterId;
        component.State = NarsiRitualsProgressState.Working;

        ritual.Effect.OnStartRitual((uid, component), ritual, EntityManager);
        UpdateRitualsState(uid, component);
    }

    private void UpdateRitualsState(EntityUid altar, NarsiAltarComponent component)
    {
        var categories = _prototype.EnumeratePrototypes<NarsiRitualCategoryPrototype>();
        var categoryUIModels = new List<NarsiRitualCategoryUIModel>();
        foreach (var category in categories)
        {
            categoryUIModels.Add
            (
                new NarsiRitualCategoryUIModel(
                    category.Name,
                    GetCategoryRitualUIModels(category, altar, component)
                )
            );
        }

        var ritualsState = new NarsiRitualsState(
            categoryUIModels,
            component.State
        );

        _ui.SetUiState(altar, NarsiRitualsInterfaceKey.Key, ritualsState);
    }

    private List<NarsiRitualUIModel> GetCategoryRitualUIModels(NarsiRitualCategoryPrototype category,
        EntityUid altar,
        NarsiAltarComponent component)
    {
        var ritualUIModels = new List<NarsiRitualUIModel>();
        foreach (var ritualId in category.Rituals)
        {
            var ritual = _prototype.Index(ritualId);
            ritualUIModels.Add
            (
                new NarsiRitualUIModel(
                    ritual.ID,
                    ritual.Name,
                    ritual.Description,
                    ritual.RequirementsDesc,
                    ritual.Effect.IsRitualAvailable((altar, component), ritual.Requirements, EntityManager) &&
                    component.State == NarsiRitualsProgressState.Idle
                )
            );
        }

        return ritualUIModels;
    }
}
