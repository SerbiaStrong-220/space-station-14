using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Shared.SS220.Kitchen.FoodProcessor;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Guidebook.Controls;

/// <summary>
/// Control for listing food processor recipes in a guidebook
/// </summary>
[UsedImplicitly]
public sealed partial class GuideFoodProcessorGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    public GuideFoodProcessorGroupEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;

        CreateEntries();
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = this;
        return true;
    }

    private void CreateEntries()
    {
        foreach (var source in _prototypes.EnumeratePrototypes<EntityPrototype>()
                     .Where(proto => !proto.Abstract)
                     .OrderBy(proto => proto.Name)
                     .ThenBy(proto => proto.ID))
        {
            if (!source.TryGetComponent(out FoodProcessorIngredientComponent? ingredient, _entities.ComponentFactory))
                continue;

            if (!_prototypes.TryIndex<EntityPrototype>(ingredient.Result, out var result))
                continue;

            AddChild(new GuideFoodProcessorEmbed(source, result, ingredient.ResultCount));
        }
    }
}
