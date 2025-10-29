// Based on https://github.com/space-syndicate/space-station-14/blob/d69a4aa3d99a04cab64c8f807fea5e983e897866/Content.Server/Corvax/GuideGenerator/ReagentEffectEntry.cs
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
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
        Description = GuidebookEffectDescriptionToWiki(effect.EntityEffectGuidebookText(protoMng, entSysMng) ?? "");
    }

    private string GuidebookEffectDescriptionToWiki(string guideBookText)
    {
        guideBookText = guideBookText.Replace("[", "<");
        guideBookText = guideBookText.Replace("]", ">");
        guideBookText = guideBookText.Replace("color", "span");

        while (guideBookText.IndexOf("<span=") != -1)
        {
            var first = guideBookText.IndexOf("<span=") + "<span=".Length - 1;
            var last = guideBookText.IndexOf(">", first);
            var replacementString = guideBookText.Substring(first, last - first);
            var color = replacementString.Substring(1);
            guideBookText = guideBookText.Replace(replacementString, string.Format(" style=\"color: {0};\"", color));
        }

        return guideBookText;
    }
}
