// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions.Events;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Experience;

/// <summary>
/// This tests ensures raising events pass to skill entity with correct order and ensures all skill condition works
/// </summary>
[TestFixture]
public sealed class SkillEntityTests
{
    [Test]
    public async Task TestSkillEntityEventsRaisingAndOrdering()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = true,
            Dirty = true
        });

        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var experienceSystem = server.System<ExperienceSystem>();

        const string entityId = "ExperienceDummyEntity";
        var effectProto = protoManager.Index<EntityPrototype>(entityId);

        var testEntity = EntityUid.Invalid;
        server.Post(() =>
        {
            testEntity = server.EntMan.Spawn(entityId);
        });

        await pair.RunTicksSync(5);

        server.Assert(() =>
        {
            var skillEntities = server.EntMan.AllEntities<SkillComponent>();

            Assert.That(skillEntities.Length, Is.EqualTo(2));

            // this comes from spawn order
            var skillEntity = skillEntities[0];

            // you name it cringe, I name it - independent check
            // so yeah just add - raise - add - raise - have fun
            server.EntMan.AddComponent<TestSkillEntityComponent>(skillEntity);

            var beforeOverrideEv = new TestSkillEntityEvent();

            server.EntMan.EventBus.RaiseLocalEvent(testEntity, ref beforeOverrideEv);
        });

        await pair.RunTicksSync(5);

        server.Assert(() =>
        {
            var skillEntities = server.EntMan.AllEntities<SkillComponent>();

            // we check if no one else spawned out of nowhere
            Assert.That(skillEntities.Length, Is.EqualTo(2));

            var skillEntity = skillEntities[0];
            var overrideSkillEntity = skillEntities[1];

            var skillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(skillEntity);

            Assert.Multiple(() =>
            {
                Assert.That(skillComp.ReceivedEvent, Is.EqualTo(true));
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(overrideSkillEntity), Is.EqualTo(false));
            });

            skillComp.ReceivedEvent = false;

            server.EntMan.AddComponent<TestSkillEntityComponent>(overrideSkillEntity);

            var afterOverrideEv = new TestSkillEntityEvent();

            server.EntMan.EventBus.RaiseLocalEvent(testEntity, ref afterOverrideEv);
        });


        await pair.RunTicksSync(5);

        server.Assert(() =>
        {
            var skillEntities = server.EntMan.AllEntities<SkillComponent>();

            // we check if no one else spawned out of nowhere
            Assert.That(skillEntities.Length, Is.EqualTo(2));

            var skillEntity = skillEntities[0];
            var overrideSkillEntity = skillEntities[1];

            var skillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(skillEntity);
            var overrideSkillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(overrideSkillEntity);

            Assert.Multiple(() =>
            {
                Assert.That(overrideSkillComp.ReceivedEvent, Is.EqualTo(true));
                Assert.That(skillComp.ReceivedEvent, Is.EqualTo(false));
            });

            overrideSkillComp.ReceivedEvent = false;
            skillComp.ReceivedEvent = false;

            server.EntMan.RemoveComponent<TestSkillEntityComponent>(overrideSkillEntity);

            var afterDeleteOverrideEv = new TestSkillEntityEvent();

            server.EntMan.EventBus.RaiseLocalEvent<TestSkillEntityEvent>(testEntity, ref afterDeleteOverrideEv);
        });

        await pair.RunTicksSync(5);

        server.Assert(() =>
        {
            var skillEntities = server.EntMan.AllEntities<SkillComponent>();

            // we check if no one else spawned out of nowhere
            Assert.That(skillEntities.Length, Is.EqualTo(2));

            var skillEntity = skillEntities[0];
            var overrideSkillEntity = skillEntities[1];

            var skillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(skillEntity);

            Assert.Multiple(() =>
            {
                Assert.That(skillComp.ReceivedEvent, Is.EqualTo(true));
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(overrideSkillEntity), Is.EqualTo(false));
            });
        });

        await pair.CleanReturnAsync();
    }
}
