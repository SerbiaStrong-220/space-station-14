using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.SS220.Kpb;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Server.Actions;

namespace Content.Server.SS220.Kpb;

public sealed partial class KpbScreenSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KpbScreenComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<KpbScreenComponent>(KpbScreenUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUIClosed);
            subs.Event<KpbScreenSelectMessage>(OnKpbScreenSelect);
            subs.Event<KpbScreenChangeColorMessage>(OnTryKpbScreenChangeColor);
            subs.Event<KpbScreenAddSlotMessage>(OnTryKpbScreenAddSlot);
            subs.Event<KpbScreenRemoveSlotMessage>(OnTryKpbScreenRemoveSlot);
        });

        SubscribeLocalEvent<KpbScreenComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<KpbScreenComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<KpbScreenComponent, KpbScreenSelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<KpbScreenComponent, KpbScreenChangeColorDoAfterEvent>(OnChangeColorDoAfter);
        SubscribeLocalEvent<KpbScreenComponent, KpbScreenRemoveSlotDoAfterEvent>(OnRemoveSlotDoAfter);
        SubscribeLocalEvent<KpbScreenComponent, KpbScreenAddSlotDoAfterEvent>(OnAddSlotDoAfter);

        InitializeKpbScreenAbilities();

    }

    private void OnOpenUIAttempt(EntityUid uid, KpbScreenComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(uid))
            args.Cancel();
    }

    private void OnKpbScreenSelect(EntityUid uid, KpbScreenComponent component, KpbScreenSelectMessage message)
    {
        if (component.Target is not { } target)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new KpbScreenSelectDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
            Marking = message.Marking,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, component.Owner, component.SelectSlotTime, doAfter, uid, target: target, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnSelectSlotDoAfter(EntityUid uid, KpbScreenComponent component, KpbScreenSelectDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        MarkingCategories category;

        switch (args.Category)
        {
            case KpbScreenCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.SetMarkingId(uid, category, args.Slot, args.Marking);

        UpdateInterface(uid, component);
    }

    private void OnTryKpbScreenChangeColor(EntityUid uid, KpbScreenComponent component, KpbScreenChangeColorMessage message)
    {
        if (component.Target is not { } target)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new KpbScreenChangeColorDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
            Colors = message.Colors,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, component.Owner, component.ChangeSlotTime, doAfter, uid, target: target, used: uid)
        {
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }
    private void OnChangeColorDoAfter(EntityUid uid, KpbScreenComponent component, KpbScreenChangeColorDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        MarkingCategories category;
        switch (args.Category)
        {
            case KpbScreenCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.SetMarkingColor(uid, category, args.Slot, args.Colors);

        // using this makes the UI feel like total ass
        // que
        // UpdateInterface(uid, component.Target, message.Session);
    }

    private void OnTryKpbScreenRemoveSlot(EntityUid uid, KpbScreenComponent component, KpbScreenRemoveSlotMessage message)
    {
        if (component.Target is not { } target)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new KpbScreenRemoveSlotDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, component.Owner, component.RemoveSlotTime, doAfter, uid, target: target, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnRemoveSlotDoAfter(EntityUid uid, KpbScreenComponent component, KpbScreenRemoveSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        MarkingCategories category;

        switch (args.Category)
        {
            case KpbScreenCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.RemoveMarking(component.Target.Value, category, args.Slot);

        _audio.PlayPvs(component.ChangeHairSound, uid);
        UpdateInterface(uid, component);
    }

    private void OnTryKpbScreenAddSlot(EntityUid uid, KpbScreenComponent component, KpbScreenAddSlotMessage message)
    {
        if (component.Target == null)
            return;

        if (message.Actor == null)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new KpbScreenAddSlotDoAfterEvent()
        {
            Category = message.Category,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Actor, component.AddSlotTime, doAfter, uid, target: component.Target.Value, used: uid)
        {
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
        _audio.PlayPvs(component.ChangeHairSound, uid);
    }
    private void OnAddSlotDoAfter(EntityUid uid, KpbScreenComponent component, KpbScreenAddSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled || !TryComp(component.Target, out HumanoidAppearanceComponent? humanoid))
            return;

        MarkingCategories category;

        switch (args.Category)
        {
            case KpbScreenCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        var marking = _markings.MarkingsByCategoryAndSpecies(category, humanoid.Species).Keys.FirstOrDefault();

        if (string.IsNullOrEmpty(marking))
            return;

        _audio.PlayPvs(component.ChangeHairSound, uid);
        _humanoid.AddMarking(uid, marking, Color.Black);

        UpdateInterface(uid, component);

    }

    private void UpdateInterface(EntityUid uid, KpbScreenComponent component)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        var hair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings)
            ? new List<Marking>(hairMarkings)
            : new();

        var facialHair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings)
            ? new List<Marking>(facialHairMarkings)
            : new();

        var state = new KpbScreenUiState(
            humanoid.Species,
            facialHair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.FacialHair) + facialHair.Count);

        component.Target = uid;
        _uiSystem.SetUiState(uid, KpbScreenUiKey.Key, state);
    }

    private void OnUIClosed(Entity<KpbScreenComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
    }

    private void OnMapInit(EntityUid uid, KpbScreenComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }
    private void OnShutdown(EntityUid uid, KpbScreenComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }

}
