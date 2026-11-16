// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Components;
using Robust.Shared.Player;

namespace Content.Client.SS220.Pathology;

public sealed partial class PathologySystem : SharedPathologySystem
{
    public event Action<EntityUid>? OnTargetUpdated;
    public event Action<EntityUid?>? OnLocalPlayerChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyHolderComponent, ComponentStartup>(OnHolderStartup);
        SubscribeLocalEvent<PathologyHolderComponent, ComponentShutdown>(OnHolderShutdown);
        SubscribeLocalEvent<PathologyHolderComponent, AfterAutoHandleStateEvent>(OnHolderStateHandled);

        SubscribeLocalEvent<SurgeryPatientComponent, ComponentStartup>(OnPatientStartup);
        SubscribeLocalEvent<SurgeryPatientComponent, ComponentShutdown>(OnPatientShutdown);
        SubscribeLocalEvent<SurgeryPatientComponent, AfterAutoHandleStateEvent>(OnPatientStateHandled);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
    }

    private void OnHolderStartup(Entity<PathologyHolderComponent> ent, ref ComponentStartup args)
        => OnTargetUpdated?.Invoke(ent.Owner);

    private void OnHolderShutdown(Entity<PathologyHolderComponent> ent, ref ComponentShutdown args)
        => OnTargetUpdated?.Invoke(ent.Owner);

    private void OnHolderStateHandled(Entity<PathologyHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshHeldGuns(ent.Owner);
        OnTargetUpdated?.Invoke(ent.Owner);
    }

    private void OnPatientStartup(Entity<SurgeryPatientComponent> ent, ref ComponentStartup args)
        => OnTargetUpdated?.Invoke(ent.Owner);

    private void OnPatientShutdown(Entity<SurgeryPatientComponent> ent, ref ComponentShutdown args)
        => OnTargetUpdated?.Invoke(ent.Owner);

    private void OnPatientStateHandled(Entity<SurgeryPatientComponent> ent, ref AfterAutoHandleStateEvent args)
        => OnTargetUpdated?.Invoke(ent.Owner);

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
        => OnLocalPlayerChanged?.Invoke(args.Entity);

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
        => OnLocalPlayerChanged?.Invoke(null);
}
