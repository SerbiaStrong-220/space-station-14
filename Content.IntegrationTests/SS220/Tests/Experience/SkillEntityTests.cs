// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using NUnit.Framework.Internal;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

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
            GetSkillEntitiesWithAssert(server, testEntity, out var skillEntity, out var overrideSkillEntity);

            server.Log.Info($"skillEntity uid is {skillEntity} and overrideSkillEntityUid is {overrideSkillEntity}");

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(overrideSkillEntity), Is.EqualTo(false));
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(overrideSkillEntity), Is.EqualTo(false));
            });

            // you name it cringe, I name it - independent check
            // so yeah just add - raise - add - raise - have fun
            server.EntMan.AddComponent<TestSkillEntityComponent>(skillEntity);

            var beforeOverrideEv = new TestSkillEntityEvent();

            server.EntMan.EventBus.RaiseLocalEvent(testEntity, ref beforeOverrideEv);
        });

        await pair.RunTicksSync(5);

        server.Assert(() =>
        {
            GetSkillEntitiesWithAssert(server, testEntity, out var skillEntity, out var overrideSkillEntity);

            server.Log.Info($"skillEntity uid is {skillEntity} and overrideSkillEntityUid is {overrideSkillEntity}");

            var skillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(skillEntity);

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(skillEntity), Is.EqualTo(true));
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(overrideSkillEntity), Is.EqualTo(false));

                Assert.That(skillComp.ReceivedEvent, Is.EqualTo(true));
            });

            skillComp.ReceivedEvent = false;

            server.EntMan.AddComponent<TestSkillEntityComponent>(overrideSkillEntity);

            var afterOverrideEv = new TestSkillEntityEvent();

            server.EntMan.EventBus.RaiseLocalEvent(testEntity, ref afterOverrideEv);
        });


        await pair.RunTicksSync(5);

        server.Assert(() =>
        {
            GetSkillEntitiesWithAssert(server, testEntity, out var skillEntity, out var overrideSkillEntity);

            var skillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(skillEntity);
            var overrideSkillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(overrideSkillEntity);

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(skillEntity), Is.EqualTo(true));
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(overrideSkillEntity), Is.EqualTo(true));

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
            GetSkillEntitiesWithAssert(server, testEntity, out var skillEntity, out var overrideSkillEntity);

            var skillComp = server.EntMan.GetComponent<TestSkillEntityComponent>(skillEntity);

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(skillEntity), Is.EqualTo(true));
                Assert.That(server.EntMan.HasComponent<TestSkillEntityComponent>(overrideSkillEntity), Is.EqualTo(false));

                Assert.That(skillComp.ReceivedEvent, Is.EqualTo(true));
            });
        });

        await pair.CleanReturnAsync();
    }

    private void GetSkillEntitiesWithAssert(IServerIntegrationInstance server, EntityUid testEntity, out EntityUid skillEntity, out EntityUid overrideSkillEntity)
    {
        var skillEntities = server.EntMan.AllEntities<SkillComponent>().Select(x => x.Owner).ToArray();

        Assert.That(server.EntMan.TryGetComponent<ExperienceComponent>(testEntity, out var experienceComponent), Is.EqualTo(true));

        var nullableSkillEntity = experienceComponent.ExperienceContainer.ContainedEntity;
        var nullableOverrideSkillEntity = experienceComponent.OverrideExperienceContainer.ContainedEntity;

        // we check if no one else spawned out of nowhere
        Assert.Multiple(() =>
        {
            Assert.That(nullableSkillEntity, Is.Not.Null);
            Assert.That(nullableOverrideSkillEntity, Is.Not.Null);

            Assert.That(skillEntities, Has.Length.EqualTo(2));

            Assert.That(nullableSkillEntity, Is.AnyOf(skillEntities));
            Assert.That(nullableOverrideSkillEntity, Is.AnyOf(skillEntities));
        });

        skillEntity = nullableSkillEntity.Value;
        overrideSkillEntity = nullableOverrideSkillEntity.Value;
    }
}
