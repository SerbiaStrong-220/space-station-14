// SS220 Changeling
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Changeling.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingBlindStingTests : GameTest
{
    private static readonly EntProtoId BlindStingAction = "ActionChangelingBlindSting";

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task BlindStingExpiresWithoutLegacyStatusEffects()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid victim = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var actions = entMan.System<SharedActionsSystem>();
            var actionUid = actions.AddAction(ling, BlindStingAction.Id)!.Value;
            var sting = new ChangelingBlindStingActionEvent
            {
                Performer = ling,
                Target = victim,
                Action = (actionUid, entMan.GetComponent<ActionComponent>(actionUid)),
            };

            entMan.EventBus.RaiseLocalEvent(ling, sting);

            Assert.Multiple(() =>
            {
                Assert.That(sting.Handled, Is.True);
                Assert.That(entMan.HasComponent<ChangelingBlindStingComponent>(victim), Is.True);
                Assert.That(entMan.GetComponent<BlindableComponent>(victim).IsBlind, Is.True);
            });

            entMan.GetComponent<ChangelingBlindStingComponent>(victim).EndTime = TimeSpan.Zero;
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingBlindStingComponent>(victim), Is.False);
                Assert.That(entMan.GetComponent<BlindableComponent>(victim).IsBlind, Is.False);
            });
        });
    }
}
