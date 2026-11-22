// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.Messenger;
using Content.Shared.PDA;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.SS220;

/// <summary>
/// The agent id card takes the messenger account of the card that was in the pda before it,
/// so the agent does not become a new contact.
/// </summary>
[TestFixture]
[TestOf(typeof(MessengerServerSystem))]
public sealed class MessengerAgentCardTest : GameTest
{
    [Test]
    public async Task AgentCardTakesAccountOfPreviousCard()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();

        EntityUid messengerServer = default;
        EntityUid pda = default;
        EntityUid agentCard = default;

        await server.WaitPost(() =>
        {
            var entMan = server.EntMan;
            messengerServer = entMan.SpawnAtPosition("PDANanotrasenMessengerServer", map.GridCoords);
            entMan.System<PowerReceiverSystem>().SetNeedsPower(messengerServer, false);
        });

        await server.WaitRunTicks(3);

        await server.WaitPost(() =>
        {
            pda = server.EntMan.SpawnAtPosition("MedicalPDA", map.GridCoords);
        });

        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            var component = server.EntMan.GetComponent<MessengerServerComponent>(messengerServer);
            Assert.That(component.Contacts, Has.Count.EqualTo(1), "The owner of the pda must be a contact");
        });

        await server.WaitPost(() =>
        {
            var containers = server.System<SharedContainerSystem>();
            var slot = containers.GetContainer(pda, PdaComponent.PdaIdSlotId);

            containers.EmptyContainer(slot);
            agentCard = server.EntMan.SpawnAtPosition("AgentIDCard", map.GridCoords);
            containers.Insert(agentCard, slot);
        });

        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            var component = server.EntMan.GetComponent<MessengerServerComponent>(messengerServer);
            var client = server.EntMan.GetComponent<MessengerClientComponent>(agentCard);

            Assert.That(component.Contacts, Has.Count.EqualTo(1), "The agent card must not create a new contact");
            Assert.That(client.IsLinked, Is.True);
            Assert.That(client.Contacts, Contains.Key(messengerServer));
        });
    }
}
