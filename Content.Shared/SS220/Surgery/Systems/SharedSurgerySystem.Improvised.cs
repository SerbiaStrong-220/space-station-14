// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Item.ItemToggle;
using Content.Shared.Kitchen.Components;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Graph.Requirements;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    private const float ImprovisedToolFailurePenalty = 0.25f;
    private const float ImprovisedToolDelayMultiplier = 1.6f;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    private static readonly ProtoId<ToolQualityPrototype> CuttingQuality = "Cutting";
    private static readonly ProtoId<ToolQualityPrototype> ScrewingQuality = "Screwing";
    private static readonly ProtoId<ToolQualityPrototype> AnchoringQuality = "Anchoring";
    private static readonly ProtoId<ToolQualityPrototype> WeldingQuality = "Welding";
    private static readonly ProtoId<ToolQualityPrototype> SlicingQuality = "Slicing";
    private static readonly ProtoId<ToolQualityPrototype> SawingQuality = "Sawing";

    public bool IsImprovisedTool(EntityUid used, SurgeryToolType toolType)
    {
        if (HasComp<SurgeryToolComponent>(used))
            return false;

        return toolType switch
        {
            SurgeryToolType.Scalpel => IsSharp(used),
            SurgeryToolType.Saw => IsSharp(used) || _tool.HasQuality(used, SawingQuality.Id),
            SurgeryToolType.Retractor => _tool.HasQuality(used, PryingQuality.Id),
            SurgeryToolType.Hemostat => _tool.HasQuality(used, CuttingQuality.Id),
            SurgeryToolType.Drill => _tool.HasQuality(used, ScrewingQuality.Id),
            SurgeryToolType.Bonesetter => _tool.HasQuality(used, AnchoringQuality.Id),
            SurgeryToolType.Cautery => _tool.HasQuality(used, WeldingQuality.Id) && _itemToggle.IsActivated(used),
            _ => false,
        };
    }

    public bool IsImprovisedForEdge(EntityUid? used, SurgeryGraphEdge edge)
    {
        if (used is not { } usedUid || HasComp<SurgeryToolComponent>(usedUid))
            return false;

        foreach (var requirement in SurgeryGraph.GetRequirements(edge))
        {
            if (requirement is SurgeryToolRequirement toolRequirement &&
                IsImprovisedTool(usedUid, toolRequirement.SurgeryTool))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSharp(EntityUid used)
    {
        return HasComp<SharpComponent>(used) || _tool.HasQuality(used, SlicingQuality.Id);
    }
}
