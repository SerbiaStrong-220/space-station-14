// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{

    private bool ValidContainerId(string containerId, EntityUid? entity = null)
    {
        if (!ContainerIds.Contains(containerId))
        {
            Log.Error($"Tried to ensure skill of entity {ToPrettyString(entity)} but skill entity container was incorrect, provided value {containerId}");
            return false;
        }

        return true;
    }
}
