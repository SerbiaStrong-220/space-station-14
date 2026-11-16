// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.Messenger;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.SS220;

/// <summary>
/// An admin ghost carries pdas with id cards, but it is not a crew member, so it must not get a messenger contact.
/// </summary>
[TestFixture]
[TestOf(typeof(MessengerServerSystem))]
public sealed class MessengerAdminGhostTest : GameTest
{
    [Test]
    public async Task AdminGhostHasNoContact()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();

        EntityUid messengerServer = default;

        await server.WaitPost(() =>
        {
            var entMan = server.EntMan;
            messengerServer = entMan.SpawnAtPosition("PDANanotrasenMessengerServer", map.GridCoords);
            entMan.System<PowerReceiverSystem>().SetNeedsPower(messengerServer, false);
        });

        await server.WaitRunTicks(3);

        await server.WaitPost(() =>
        {
            server.EntMan.SpawnAtPosition("AdminObserver", map.GridCoords);
        });

        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            var component = server.EntMan.GetComponent<MessengerServerComponent>(messengerServer);
            var names = new List<string>();

            foreach (var contact in component.Contacts.Values)
            {
                names.Add($"'{contact.Name}'");
            }

            var containers = server.System<SharedContainerSystem>();
            var query = server.EntMan.EntityQueryEnumerator<MessengerClientComponent>();

            while (query.MoveNext(out var uid, out _))
            {
                var owner = containers.TryGetContainingContainer(uid, out var container) ? container.Owner : default;
                names.Add($"client {server.EntMan.ToPrettyString(uid)} in {server.EntMan.ToPrettyString(owner)}");
            }

            Assert.That(component.Contacts, Is.Empty, $"Contacts: {string.Join(", ", names)}");
        });
    }
}
