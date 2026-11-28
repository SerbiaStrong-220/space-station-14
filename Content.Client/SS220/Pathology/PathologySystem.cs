// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;

namespace Content.Client.SS220.Pathology;

public sealed partial class PathologySystem : SharedPathologySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyHolderComponent, AfterAutoHandleStateEvent>(OnHolderStateHandled);

        InitializeDollAlert();
    }

    private void OnHolderStateHandled(Entity<PathologyHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshHeldGuns(ent.Owner);
    }
}
