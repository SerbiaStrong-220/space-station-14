// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph;

public sealed class SurgeryGraphSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public SoundSpecifier? GetSoundSpecifier(SurgeryGraphEdge edge)
    {
        return Get(edge, (x) => x.EndSound);
    }

    public IReadOnlyList<ISurgeryGraphAction> GetActions(SurgeryGraphEdge edge)
    {
        return GetList(edge, (x) => x.Actions);
    }

    public IReadOnlyList<ISurgeryGraphCondition> GetConditions(SurgeryGraphEdge edge)
    {
        return GetList(edge, (x) => x.Conditions);
    }

    public string? ExamineDescription(SurgeryGraphNode node)
    {
        return Get(node, (x) => x.NodeText.ExamineDescription);
    }

    public string? Description(SurgeryGraphNode node)
    {
        return Get(node, (x) => x.NodeText.Description);
    }

    public string? Popup(SurgeryGraphNode node)
    {
        return Get(node, (x) => x.NodeText.Popup);
    }

    public float? Delay(SurgeryGraphEdge edge)
    {
        return Get(edge, (x) => x.Delay);
    }

    public IReadOnlyList<T> GetList<T>(SurgeryGraphEdge edge, Func<SurgeryGraphEdge, IReadOnlyList<T>> listGetter) where T : class
    {
        if (edge.BaseEdge.HasValue
            && listGetter(edge).Count == 0
            && _prototypeManager.TryIndex(edge.BaseEdge, out var baseEdgeProto))
            return listGetter(baseEdgeProto.Edge);

        return listGetter(edge);
    }

    public T? Get<T>(SurgeryGraphNode node, Func<SurgeryGraphNode, T?> getter)
    {
        if (getter(node) != null)
            return getter(node);

        if (_prototypeManager.TryIndex(node.BaseNode, out var prototype))
            return getter(prototype.Node);

        return default;
    }

    public T? Get<T>(SurgeryGraphEdge edge, Func<SurgeryGraphEdge, T?> getter)
    {
        if (getter(edge) != null)
            return getter(edge);

        if (_prototypeManager.TryIndex(edge.BaseEdge, out var prototype))
            return getter(prototype.Edge);

        return default;
    }
}
