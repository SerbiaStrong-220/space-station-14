// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.SS220.Forensics.Serialization;

/// <summary>
/// Reads individual material profiles while accepting and discarding scalar descriptions without source identities.
/// </summary>
public sealed class StorageMicroFiberTraceSetSerializer
    : ITypeReader<HashSet<StorageMicroFiberTrace>, SequenceDataNode>
{
    public HashSet<StorageMicroFiberTrace> Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<HashSet<StorageMicroFiberTrace>>? instanceProvider = null)
    {
        HashSet<StorageMicroFiberTrace> traces = instanceProvider != null ? instanceProvider() : [];
        foreach (var traceNode in node.Sequence)
        {
            if (traceNode is ValueDataNode)
                continue;

            traces.Add(serializationManager.Read<StorageMicroFiberTrace>(traceNode, hookCtx, context));
        }

        return traces;
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        List<ValidationNode> validatedTraces = [];
        foreach (var traceNode in node.Sequence)
        {
            if (traceNode is ValueDataNode)
            {
                validatedTraces.Add(new ValidatedValueNode(traceNode));
                continue;
            }

            validatedTraces.Add(serializationManager.ValidateNode<StorageMicroFiberTrace>(traceNode, context));
        }

        return new ValidatedSequenceNode(validatedTraces);
    }
}
