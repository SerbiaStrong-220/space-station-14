// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.SS220.CultYogg.MiGo;

public sealed class SharedMiGoPlantSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<MiGoComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    public void OpenUI(Entity<MiGoComponent> entity, ActorComponent actor)
    {
        _userInterfaceSystem.TryToggleUi(entity.Owner, MiGoUiKey.Plant, actor.PlayerSession);
    }
}
