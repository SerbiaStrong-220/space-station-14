using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.SS220.Wiki.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.GuideGenerator;

public sealed class ReactionJsonGenerator
{
    // SS220 Wiki begin
    [ValidatePrototypeId<MixingCategoryPrototype>]
    private const string DefaultMixingCategory = "DummyMix";
    // SS220 Wiki end

    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();

        var reactions =
            prototype
                .EnumeratePrototypes<ReactionPrototype>()
                .Select(x => new ReactionWikiEntry(x))
                .ToDictionary(x => x.Id, x => x);

        // SS220 Wiki begin
        if (reactions != null)
            AddMixingCategories(reactions, prototype);
        // SS220 Wiki end

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals, // SS220 Wiki
            Converters =
            {
                new UniversalJsonConverter<EntityEffect>(),
            }
        };

        file.Write(JsonSerializer.Serialize(reactions, serializeOptions));
    }

    // SS220 Wiki
    // Based on https://github.com/space-syndicate/space-station-14/blob/d69a4aa3d99a04cab64c8f807fea5e983e897866/Content.Server/Corvax/GuideGenerator/ReactionJsonGenerator.cs#L11-L34
    private static void AddMixingCategories(Dictionary<string, ReactionWikiEntry> reactions, IPrototypeManager prototype)
    {
        foreach (var reaction in reactions)
        {
            var reactionPrototype = prototype.Index<ReactionPrototype>(reaction.Key);
            var mixingCategories = new List<MixingCategoryPrototype>();
            if (reactionPrototype.MixingCategories != null)
            {
                foreach (var category in reactionPrototype.MixingCategories)
                {
                    mixingCategories.Add(prototype.Index(category));
                }
            }
            else
            {
                mixingCategories.Add(prototype.Index<MixingCategoryPrototype>(DefaultMixingCategory));
            }

            foreach (var mixingCategory in mixingCategories)
            {
                reactions[reaction.Key].MixingCategories.Add(new MixingCategoryWikiEntry(mixingCategory));
            }
        }
    }
    // SS220 Wiki end
}

