using Content.Server.Ghost;
using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.SS220.GhostExtension;
public sealed class GhostExtensionSystem : EntitySystem
{
    public override void Initialize()
    {
        /*SubscribeLocalEvent<GhostEvent>
        SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnMindSturtup);
        SubscribeAllEvent<MindContainerComponent, SuicideEvent>(OnSuicide);*/
        SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnGhostStartup, before: [typeof(GhostSystem)]);
        //SubscribeLocalEvent<GhostComponent, MapInitEvent>(OnMapInit);
        //SubscribeLocalEvent<GhostComponent, ComponentShutdown>(OnGhostShutdown);
        SubscribeNetworkEvent<GhostReturnToBodyRequest>(OnGhostReturnToBodyRequest);
    }

    private void OnGhostStartup(Entity<GhostComponent> ent, ref ComponentStartup args)
    {

    }

    private void OnSuicide(Entity<MindContainerComponent> ent, ref SuicideEvent args)
    {

    }

    private void OnMindSturtup(Entity<GhostComponent> ent, ref ComponentStartup args)
    {

    }

    private void OnGhostReturnToBodyRequest(GhostReturnToBodyRequest ev, EntitySessionEventArgs args)
    {
        //throw new NotImplementedException();
    }

    /*private void OnGhostShutdown(Entity<GhostComponent> ent, ref ComponentShutdown args)
    {
        //throw new NotImplementedException();
    }

    private void OnMapInit(Entity<GhostComponent> ent, ref MapInitEvent args)
    {
        //throw new NotImplementedException();
    }

    private void OnGhostStartup(Entity<GhostComponent> ent, ref ComponentStartup args)
    {
        //throw new NotImplementedException();
    }*/
}
