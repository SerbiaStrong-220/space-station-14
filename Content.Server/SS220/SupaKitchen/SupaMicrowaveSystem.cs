using Content.Server.Body.Systems;
using Content.Server.Kitchen.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.SS220.SupaKitchen;
public sealed class SupaMicrowaveSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly CookingMachineSystem _cookingMachine = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupaMicrowaveComponent, ProcessedInCookingMachineEvent>(OnItemProcessed);
        SubscribeLocalEvent<SupaMicrowaveComponent, SuicideEvent>(OnSuicide);
    }

    private void OnItemProcessed(EntityUid uid, SupaMicrowaveComponent component, ProcessedInCookingMachineEvent args)
    {
        var ev = new BeingMicrowavedEvent(args.Item, args.User);
        RaiseLocalEvent(args.Item, ev);

        if (ev.Handled)
        {
            args.Handled = true;
            return;
        }

        // destroy microwave
        if (_tag.HasTag(args.Item, "MicrowaveMachineUnsafe") || _tag.HasTag(args.Item, "Metal"))
        {
            _cookingMachine.Break(uid, args.CookingMachine);
            args.Handled = true;
            return;
        }

        if (!TryComp<CookingInstrumentComponent>(uid, out var instrument))
            return;

        if (_tag.HasTag(args.Item, "MicrowaveSelfUnsafe") || _tag.HasTag(args.Item, "Plastic"))
        {
            var junk = Spawn(instrument.FailureResult, Transform(uid).Coordinates);
            args.CookingMachine.Storage.Insert(junk);
            QueueDel(args.Item);
        }
    }

    private void OnSuicide(EntityUid uid, SupaMicrowaveComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<CookingMachineComponent>(uid, out var cookingMachine))
            return;

        args.SetHandled(SuicideKind.Heat);
        var victim = args.Victim;
        var headCount = 0;

        if (TryComp<BodyComponent>(victim, out var body))
        {
            var headSlots = _bodySystem.GetBodyChildrenOfType(victim, BodyPartType.Head, body);

            foreach (var part in headSlots)
            {
                cookingMachine.Storage.Insert(part.Id);
                headCount++;
            }
        }

        var othersMessage = headCount > 1
            ? Loc.GetString("microwave-component-suicide-multi-head-others-message", ("victim", victim))
            : Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));

        var selfMessage = headCount > 1
            ? Loc.GetString("microwave-component-suicide-multi-head-message")
            : Loc.GetString("microwave-component-suicide-message");

        _popupSystem.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
        _popupSystem.PopupEntity(selfMessage, victim, victim);

        _audio.PlayPvs(cookingMachine.ClickSound, uid, AudioParams.Default.WithVolume(-2));
        cookingMachine.CookingTimer = 10;

        _cookingMachine.StartCooking(uid, cookingMachine, args.Victim);
    }
}
