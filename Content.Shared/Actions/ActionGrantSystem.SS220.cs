// SS220 Changeling
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

public sealed partial class ActionGrantSystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    /// <summary>
    /// Moves an action owned by an <see cref="ActionGrantComponent"/> to another entity while preserving
    /// both grant components' shutdown bookkeeping. Grants a fresh action when the source has not created it.
    /// </summary>
    public bool TryTransferGrantedAction(
        Entity<ActionGrantComponent?> source,
        EntityUid target,
        EntProtoId actionPrototype,
        out EntityUid actionEntity)
    {
        EntityUid? transferredAction = null;
        if (Resolve(source, ref source.Comp, false))
        {
            foreach (var action in source.Comp.ActionEntities.ToArray())
            {
                if (!Exists(action) || MetaData(action).EntityPrototype?.ID != actionPrototype.Id)
                    continue;

                transferredAction = action;
                source.Comp.ActionEntities.Remove(action);
                _actionContainer.TransferActionWithNewAttached(action, target, target);
                break;
            }

            source.Comp.Actions.RemoveAll(proto => proto.Id == actionPrototype.Id);
            Dirty(source);
        }

        if (transferredAction == null)
            _actions.AddAction(target, ref transferredAction, actionPrototype);

        if (transferredAction is not { } result)
        {
            actionEntity = default;
            return false;
        }

        var targetGrant = EnsureComp<ActionGrantComponent>(target);
        if (!targetGrant.Actions.Any(proto => proto.Id == actionPrototype.Id))
            targetGrant.Actions.Add(actionPrototype);
        if (!targetGrant.ActionEntities.Contains(result))
            targetGrant.ActionEntities.Add(result);
        Dirty(target, targetGrant);
        actionEntity = result;
        return true;
    }
}
