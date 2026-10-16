// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Kitchen.FoodProcessor;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Kitchen.FoodProcessor;

public sealed class FoodProcessorSystem : SharedFoodProcessorSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedPowerStateSystem _powerState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodProcessorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FoodProcessorComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<FoodProcessorComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<FoodProcessorComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<FoodProcessorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FoodProcessorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
        SubscribeLocalEvent<FoodProcessorComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FoodProcessorComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var component, out var power))
        {
            if(!power.Powered || !IsProcessing(uid)) // Its electric!
                continue;

            if(component.RemainingProcessingTime > 0)
                component.RemainingProcessingTime -= frameTime;

            if (component.RemainingProcessingTime <= 0)
                FinishProcessing((uid, component));
        }
    }

    private void OnComponentInit(Entity<FoodProcessorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.InputContainer =
            _container.EnsureContainer<Container>(ent.Owner, FoodProcessorComponent.InputContainerId);
    }

    private void OnComponentShutdown(Entity<FoodProcessorComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
    }

    private void OnInsertAttempt(Entity<FoodProcessorComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container.ID != FoodProcessorComponent.InputContainerId ||
            HasComp<FoodProcessorIngredientComponent>(args.EntityUid))
        {
            return;
        }

        args.Cancel();
    }

    private void OnRemoveAttempt(Entity<FoodProcessorComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (args.Cancelled || args.Container.ID != FoodProcessorComponent.InputContainerId)
            return;

        args.Cancel();
    }

    private void OnInteractUsing(Entity<FoodProcessorComponent> ent, ref InteractUsingEvent args)
    {
        // Another interaction handler already dealt with this click.
        if (args.Handled)
            return;

        // Ingredients cannot be added after a processing cycle has started.
        if (IsProcessing(ent.AsNullable()))
        {
            _popup.PopupEntity(Loc.GetString("food-processor-popup-busy"), ent, args.User);
            args.Handled = true;
            return;
        }

        // Only entities with a food processor recipe are valid ingredients.
        if (!HasComp<FoodProcessorIngredientComponent>(args.Used))
        {
            _popup.PopupEntity(Loc.GetString("food-processor-popup-invalid"), ent, args.User);
            return;
        }

        // Do not insert more ingredient entities than the processor can hold.
        if (ent.Comp.InputContainer.ContainedEntities.Count >= ent.Comp.Capacity)
        {
            _popup.PopupEntity(Loc.GetString("food-processor-popup-full"), ent, args.User);
            args.Handled = true;
            return;
        }

        // The container system can still reject insertion for general container rules.
        if (!_container.Insert(args.Used, ent.Comp.InputContainer))
            return;

        args.Handled = true;
    }

    private void OnGetAlternativeVerb(Entity<FoodProcessorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Do not start another cycle while the processor is already running.
        if (IsProcessing(ent.AsNullable()))
        {
            _popup.PopupEntity(Loc.GetString("food-processor-popup-busy"), ent, args.User);
            return;
        }

        // A processing cycle needs at least one inserted ingredient.
        if (ent.Comp.InputContainer.ContainedEntities.Count == 0)
            return;

        // Processing cannot start unless the machine is receiving power.
        if (!_power.IsPowered(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("food-processor-popup-no-power"), ent, args.User);
            return;
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("food-processor-start-processing"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Priority = 1,
            Act = () =>
            {
                StartProcessing(ent);
            }
        });
    }

    private void OnGetVerbs(Entity<FoodProcessorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Ingredients cannot be ejected while they are being processed.
        if (IsProcessing(ent.AsNullable()))
            return;

        // Processor is empty, nothing to eject
        if (ent.Comp.InputContainer.ContainedEntities.Count == 0)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("food-processor-verb-eject"),
            Category = VerbCategory.Eject,
            Act = () => EjectIngredients(ent),
        });
    }

    private void EjectIngredients(Entity<FoodProcessorComponent> ent)
    {
        // Recheck because the state could change while the context menu is open.
        if (IsProcessing(ent.AsNullable()))
            return;

        _container.EmptyContainer(
            ent.Comp.InputContainer,
            force: true,
            destination: Transform(ent).Coordinates);
    }

    private bool IsProcessing(Entity<FoodProcessorComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.RemainingProcessingTime > 0f;
    }

    private void StartProcessing(Entity<FoodProcessorComponent> ent)
    {
        var ingredientCount = ent.Comp.InputContainer.ContainedEntities.Count;
        ent.Comp.RemainingProcessingTime = Math.Max(ent.Comp.ProcessingTime * ingredientCount, float.Epsilon);
        _jitter.AddJitter(ent, -10, 100);
        _powerState.TrySetWorkingState(ent.Owner, true);
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.ProcessingSound, ent)?.Entity;
    }

    private void FinishProcessing(Entity<FoodProcessorComponent> ent)
    {
        ent.Comp.RemainingProcessingTime = 0f;
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
        RemCompDeferred<JitteringComponent>(ent);
        _powerState.TrySetWorkingState(ent.Owner, false);

        foreach (var ingredient in ent.Comp.InputContainer.ContainedEntities.ToList())
        {
            if (!TryComp<FoodProcessorIngredientComponent>(ingredient, out var recipe))
                continue;

            for (var i = 0; i < recipe.ResultCount; i++)
            {
                Spawn(recipe.Result, Transform(ent).Coordinates);
            }

            _container.Remove(ingredient, ent.Comp.InputContainer, reparent: false, force: true);
            QueueDel(ingredient);
        }
    }
}
