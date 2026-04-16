using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Robust.Shared.Containers;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    private void InitializeMachines()
    {
        SubscribeLocalEvent<MachineComponent, ComponentInit>(OnMachineInit);
        SubscribeLocalEvent<MachineComponent, MapInitEvent>(OnMachineMapInit);
    }

    private void OnMachineInit(EntityUid uid, MachineComponent component, ComponentInit args)
    {
        component.BoardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        component.PartContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);
    }

    private void OnMachineMapInit(EntityUid uid, MachineComponent component, MapInitEvent args)
    {
        CreateBoardAndStockParts(uid, component);
        RefreshParts(uid, component);
    }

    private void OnMachineExaminableVerb(EntityUid uid, MachineComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var markup = new FormattedMessage();
        RaiseLocalEvent(uid, new UpgradeExamineEvent(ref markup));
        if (markup.IsEmpty)
            return; // Not upgradable.

        markup = FormattedMessage.FromMarkup(markup.ToMarkup().TrimEnd('\n')); // Cursed workaround to https://github.com/space-wizards/RobustToolbox/issues/3371

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                _examineSystem.SendExamineTooltip(args.User, uid, markup, getVerbs: false, centerAtCursor: false);
            },
            Text = Loc.GetString("machine-upgrade-examinable-verb-text"),
            Message = Loc.GetString("machine-upgrade-examinable-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public List<MachinePartComponent> GetAllParts(EntityUid uid, MachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new List<MachinePartComponent>();

        return GetAllParts(component);
    }

    public List<MachinePartComponent> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartComponent>();

        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (TryComp<MachinePartComponent>(entity, out var machinePart))
                parts.Add(machinePart);
        }

        return parts;
    }

    public Dictionary<string, float> GetPartsRatings(List<MachinePartComponent> parts)
    {
        var output = new Dictionary<string, float>();
        foreach (var type in _prototypeManager.EnumeratePrototypes<MachinePartPrototype>())
        {
            var amount = 0f;
            var sumRating = 0f;
            foreach (var part in parts.Where(part => part.PartType == type.ID))
            {
                amount++;
                sumRating += part.Rating;
            }
            var rating = amount != 0 ? sumRating / amount : 0;
            output.Add(type.ID, rating);
        }

        return output;
    }

    public void RefreshParts(EntityUid uid, MachineComponent component)
    {
        var parts = GetAllParts(component);
        EntityManager.EventBus.RaiseLocalEvent(uid, new RefreshPartsEvent
        {
            Parts = parts,
            PartRatings = GetPartsRatings(parts),
        }, true);
    }
    private void CreateBoardAndStockParts(EntityUid uid, MachineComponent component)
    {
        // Entity might not be initialized yet.
        var boardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        var partContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);

        if (string.IsNullOrEmpty(component.Board))
            return;

        // We're done here, let's suppose all containers are correct just so we don't screw SaveLoadSave.
        if (boardContainer.ContainedEntities.Count > 0)
            return;

        var xform = Transform(uid);
        if (!TrySpawnInContainer(component.Board, uid, MachineFrameComponent.BoardContainerName, out var board))
        {
            throw new Exception($"Couldn't insert board with prototype {component.Board} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}!");
        }

        if (!TryComp<MachineBoardComponent>(board, out var machineBoard))
        {
            throw new Exception($"Entity with prototype {component.Board} doesn't have a {nameof(MachineBoardComponent)}!");
        }

        foreach (var (stackType, amount) in machineBoard.StackRequirements)
        {
            var stack = _stackSystem.Spawn(amount, stackType, xform.Coordinates);
            if (!_container.Insert(stack, partContainer))
                throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
        }

        foreach (var (compName, info) in machineBoard.ComponentRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                if(!TrySpawnInContainer(info.DefaultPrototype, uid, MachineFrameComponent.PartContainerName, out _))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{compName}' to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            }
        }

        foreach (var (tagName, info) in machineBoard.TagRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                if(!TrySpawnInContainer(info.DefaultPrototype, uid, MachineFrameComponent.PartContainerName, out _))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{tagName}' to machine with prototype {Prototype(uid)?.ID ?? "N/A"}");
            }
        }
    }
}
