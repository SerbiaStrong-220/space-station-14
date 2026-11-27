using System.Linq;
using System.Text;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.CreatureEgg;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.CreatureEgg;

public sealed class NarsiCreatureEggSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiCreatureEggComponent, EntInsertedIntoContainerMessage>(OnInsertedToContainer);
        SubscribeLocalEvent<NarsiCreatureEggComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<NarsiCreatureEggComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, NarsiCreatureEggComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!IsAllContainersFull(uid, out var emptySlots))
        {
            var builder = new StringBuilder();
            builder.AppendLine(Loc.GetString("narsi-creature-egg-add-parts"));

            for (int i = 0; i < emptySlots.Count; i++)
            {
                var slot = emptySlots[i];
                builder.Append("[color=red]" + Loc.GetString(slot) + "[/color]");
                builder.Append(i + 1 == emptySlots.Count ? "." : ", ");
            }

            args.PushMarkup(builder.ToString());

            return;
        }

        SetCreatureNameMarkup((uid, component), args);
    }

    private void SetCreatureNameMarkup(Entity<NarsiCreatureEggComponent> egg, ExaminedEvent args)
    {
        var step = egg.Comp.CurrentStep;
        if (step?.EntityProtoId == null || !_prototype.TryIndex<EntityPrototype>(step.EntityProtoId, out var proto))
        {
            args.PushMarkup(Loc.GetString("narsi-creature-egg-no-creature"));
            return;
        }

        args.PushMarkup(Loc.GetString("narsi-creature-egg-creature-inside", ("entity", proto.Name)));
    }

    private bool IsAllContainersFull(EntityUid uid, out List<string> emptySlots)
    {
        emptySlots = new List<string>();

        if (!TryComp<ItemSlotsComponent>(uid, out var slots))
            return false;

        foreach (var (_, slot) in slots.Slots)
        {
            if (slot.ContainerSlot?.ContainedEntity == null)
            {
                emptySlots.Add(slot.Name);
            }
        }

        return emptySlots.Count == 0;
    }

    private void OnGetVerb(EntityUid uid, NarsiCreatureEggComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (component.CurrentStep?.EntityProtoId == null)
            return;

        var verb = new Verb
        {
            Text = Loc.GetString("narsi-creature-egg-get-out-creature"),
            Icon = new SpriteSpecifier.Rsi(
                new ResPath("/Textures/SS220/DarkForces/Cult/Structures/creature_egg.rsi"), "stage-1"),
            Act = () => CreateCreature((uid, component))
        };
        args.Verbs.Add(verb);
    }

    private void OnInsertedToContainer(EntityUid uid, NarsiCreatureEggComponent component,
        EntInsertedIntoContainerMessage args)
    {
        if (!IsAllContainersFull(uid, out _))
            return;

        UpdateNextCreatureStep((uid, component));
    }

    private void UpdateNextCreatureStep(Entity<NarsiCreatureEggComponent> egg)
    {
        var component = egg.Comp;
        if (component.CurrentStep == null)
        {
            var newStep = component.CreatureSteps.FirstOrDefault();
            if (newStep == null)
                return;

            SetCreatureStep(egg, newStep);
            return;
        }

        var index = component.CreatureSteps.IndexOf(component.CurrentStep);
        if (index + 1 >= component.CreatureSteps.Count)
        {
            CreateCreature(egg);
            return;
        }

        SetCreatureStep(egg, component.CreatureSteps[index + 1]);
    }

    private void SetCreatureStep(Entity<NarsiCreatureEggComponent> egg, CreatureStep newStep)
    {
        var component = egg.Comp;

        component.CurrentStep = newStep;
        component.CreatureNextStepTick = _timing.CurTime + newStep.Delay;

        _appearanceSystem.SetData(egg, CreatureVisuals.Egg, newStep.Stage);
    }

    private void CreateCreature(Entity<NarsiCreatureEggComponent> egg)
    {
        var entProtoId = egg.Comp.CurrentStep?.EntityProtoId;
        if (entProtoId == null)
        {
            QueueDel(egg);
            return;
        }

        var coords = Transform(egg).Coordinates;
        Spawn(entProtoId.Value, coords);
        QueueDel(egg);

        RaiseLocalEvent(new NarsiCreatureEggSpawnEvent(entProtoId.Value));
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NarsiCreatureEggComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CreatureNextStepTick == TimeSpan.Zero || component.CreatureNextStepTick > _timing.CurTime)
                continue;

            UpdateNextCreatureStep((uid, component));
        }
    }
}
