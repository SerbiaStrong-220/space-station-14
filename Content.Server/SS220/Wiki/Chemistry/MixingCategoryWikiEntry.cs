// Based on https://github.com/space-syndicate/space-station-14/blob/d69a4aa3d99a04cab64c8f807fea5e983e897866/Content.Server/Corvax/GuideGenerator/MixingCategoryEntry.cs
using Content.Shared.Chemistry.Reaction;
using System.Text.Json.Serialization;

namespace Content.Server.SS220.Wiki.Chemistry;

public sealed class MixingCategoryWikiEntry
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("id")]
    public string Id { get; }

    public MixingCategoryWikiEntry(MixingCategoryPrototype proto)
    {
        Name = Loc.GetString(proto.VerbText);
        Id = proto.ID;
    }
}
