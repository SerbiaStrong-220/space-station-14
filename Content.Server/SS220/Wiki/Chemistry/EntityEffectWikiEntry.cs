// Based on https://github.com/space-syndicate/space-station-14/blob/d69a4aa3d99a04cab64c8f807fea5e983e897866/Content.Server/Corvax/GuideGenerator/ReagentEffectEntry.cs
using Content.Shared.EntityEffects;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text.Json.Serialization;

namespace Content.Server.SS220.Wiki.Chemistry;

public sealed class EntityEffectWikiEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("description")]
    public string Description { get; }

    public EntityEffectWikiEntry(EntityEffect effect)
    {
        var protoMng = IoCManager.Resolve<IPrototypeManager>();
        var entSysMng = IoCManager.Resolve<IEntitySystemManager>();

        Id = effect.GetType().Name;
        Description = GetEntityEffectWikiDescription(effect, protoMng, entSysMng);
    }

    public static string GetEntityEffectWikiDescription(EntityEffect effect, IPrototypeManager protoMng, IEntitySystemManager entSysMng)
    {
        if (effect.EntityEffectGuidebookText(protoMng, entSysMng) is not { } description)
            return string.Empty;

        description = Loc.GetString("wiki-entity-effect-description",
            ("effect", description),
            ("chance", effect.Probability),
            ("conditionCount", effect.Conditions?.Length ?? 0),
            ("conditions",
                ContentLocalizationManager.FormatList(
                    effect.Conditions?.Select(x => x.EntityConditionGuidebookText(protoMng)).ToList() ?? []
                )));

        return GuidebookEffectDescriptionToWiki(description);
    }

    private static string GuidebookEffectDescriptionToWiki(string guideBookText)
    {
        guideBookText = guideBookText.Replace("[", "<");
        guideBookText = guideBookText.Replace("]", ">");
        guideBookText = guideBookText.Replace("color", "span");

        while (guideBookText.Contains("<span=", StringComparison.CurrentCulture))
        {
            var first = guideBookText.IndexOf("<span=") + "<span=".Length - 1;
            var last = guideBookText.IndexOf('>', first);
            var replacementString = guideBookText[first..last];
            var color = replacementString[1..];
            guideBookText = guideBookText.Replace(replacementString, string.Format(" style=\"color: {0};\"", color));
        }

        return guideBookText;
    }
}
