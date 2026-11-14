// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Telepathy;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Server.Zombies;

public sealed partial class ZombieSystem
{
    private static readonly ProtoId<TelepathyChannelPrototype> ZombieTelepathyChannel = "TelepathyChannelZombie";

    private void InitializeTelepathy()
    {
        SubscribeLocalEvent<InitialInfectedComponent, ComponentStartup>(OnInitialInfectedStartup);
    }

    private void OnInitialInfectedStartup(Entity<InitialInfectedComponent> ent, ref ComponentStartup args)
    {
        AddZombieTelepathy(ent, canSend: true);
    }

    private void AddZombieTelepathy(EntityUid uid, bool canSend)
    {
        if (HasComp<TelepathyComponent>(uid))
            return;

        AddComp(uid, new TelepathyComponent { CanSend = canSend, TelepathyChannelPrototype = ZombieTelepathyChannel });
    }
}
