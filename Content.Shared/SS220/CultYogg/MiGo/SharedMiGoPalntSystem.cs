// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Player;

namespace Content.Shared.SS220.CultYogg.MiGo;

public sealed partial class SharedMiGoPlantSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterfaceSystem = default!;

    public void OpenUI(Entity<MiGoComponent> entity, ActorComponent actor)
    {
        _userInterfaceSystem.TryToggleUi(entity.Owner, MiGoUiKey.Plant, actor.PlayerSession);
    }
}
