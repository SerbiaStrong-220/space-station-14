// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.MindExtension;

public sealed class MindExtensionData
{
    public Dictionary<NetEntity, TrailPointMetaData> Trail { get; } = new();
    public TimeSpan? RespawnTimer { get; set; }
    public bool RespawnAvailable { get; set; } = true;
    public TimeSpan RespawnTime { get; } = TimeSpan.FromMinutes(15);
    public EntityUid? CurrentBody { get; set; }
}
