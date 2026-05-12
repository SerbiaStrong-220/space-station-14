// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.SS220.ResourceMiner;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.ResourceMiner;

public sealed class ResourceMinerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string BaseStatId = "base";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResourceMinerComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleStateEvent);
    }

    private void OnAfterAutoHandleStateEvent(Entity<ResourceMinerComponent> entity, ref AfterAutoHandleStateEvent _)
    {
        _sprite.LayerSetRsiState(entity.Owner, 0, entity.Comp.Silo is null ? BaseStatId : entity.Comp.TurnOnState);
    }
}
