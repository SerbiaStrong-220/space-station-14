using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Disposal.Unit;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.SS220.Felinid.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.SS220.Tests.Felinid;

/// <summary>
/// Checks the client's own prediction for a crawler inside a pipe.
/// </summary>
public sealed class FelinidPipecrawlClientTest : InteractionTest
{
    protected override string PlayerPrototype => "MobFelinid";

    /// <summary>
    /// Shooting and swinging at nothing are attacks without a target. If the client does not know the
    /// crawler's current tube, it allows them, predicts the shot and its recoil, and the server rejects it.
    /// </summary>
    [Test]
    public async Task ClientCannotAttackFromInsidePipe()
    {
        await Server.WaitPost(() =>
        {
            var coordinates = MapData.GridCoords;
            var transforms = SEntMan.System<SharedTransformSystem>();
            var unit = SEntMan.SpawnEntity("DisposalUnit", coordinates);
            var entry = SEntMan.SpawnEntity("DisposalTrunk", coordinates);
            foreach (var uid in new[] { unit, entry })
            {
                if (!SEntMan.GetComponent<TransformComponent>(uid).Anchored)
                    transforms.AnchorEntity(uid);
            }

            Assert.That(SEntMan.System<DisposalUnitSystem>().TryInsert(unit, SPlayer, null), Is.True);

            var pipecrawl = SEntMan.GetComponent<DisposalPipeCrawlerComponent>(SPlayer);
            var action = pipecrawl.ActionEntity!.Value;
            SEntMan.System<SharedActionsSystem>()
                .PerformAction(SPlayer, (action, SEntMan.GetComponent<ActionComponent>(action)));
            Assert.That(pipecrawl.InsidePipe, Is.True);
        });

        await RunTicks(5);

        await Client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(CEntMan.GetComponent<DisposalPipeCrawlerComponent>(CPlayer).InsidePipe, Is.True);
                Assert.That(CEntMan.System<ActionBlockerSystem>().CanAttack(CPlayer), Is.False);
            });
        });
    }
}
