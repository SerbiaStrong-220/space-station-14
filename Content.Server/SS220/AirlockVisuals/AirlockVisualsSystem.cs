using Content.Shared.Doors.Components;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.AirlockVisuals;

public sealed partial class AirlockVisualsSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AirlockVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<AirlockVisualsComponent> ent, ref ComponentInit args)
    {
        _appearance.SetData(ent.Owner, DoorVisuals.ClosedLights, true);
    }
}
