// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Components;

public sealed partial class JobBackgroundSublevelAddComponent
{
    // Server-side purchase order, needed when a temporary antagonist bonus is revoked.
    public readonly List<ExperienceSublevelPurchase> RemoveAntagPurchases = new();
}

public sealed class ExperienceSublevelPurchase
{
    public readonly ProtoId<SkillTreePrototype> SkillTree;
    public int Amount;

    public ExperienceSublevelPurchase(ProtoId<SkillTreePrototype> skillTree, int amount)
    {
        SkillTree = skillTree;
        Amount = amount;
    }
}
