// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.IO;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Chat;
using Content.Server.Cuffs;
using Content.Server.Hands.Systems;
using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Silence;
using Content.Server.SS220.DarkForces.Saint.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Speech.Muting;
using Content.Shared.SS220.DarkForces.Narsi.Cultist.Muzzle;
using Content.Shared.SS220.FastUI;
using Content.Server.SS220.DarkForces.Narsi.Progress;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Components;
using Content.Shared.Mobs;
using Content.Shared.Actions.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.SS220.DarkForces.Saint.Chaplain.Events;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.IntegrationTests.SS220.Tests.DarkForces;

public sealed class CultPortTest : GameTest
{
    private static readonly EntProtoId NarsiPrototype = "Narsie";
    private static readonly ProtoId<SecretListingCategoryPrototype> RuneCategory = "NarsiRuneListing";

    [Test]
    public async Task LeaderIsReplacedAfterDeathRemovalAndConversion()
    {
        var map = await Pair.CreateTestMap();
        var progressSystem = SEntMan.System<NarsiCultProgressSystem>();
        var mobState = SEntMan.System<MobStateSystem>();
        EntityUid first = default;
        EntityUid second = default;
        NarsiCultProgressComponent progress = null;

        await Server.WaitAssertion(() =>
        {
            progressSystem.CreateProgress();
            progress = SEntMan.EntityQuery<NarsiCultProgressComponent>().Single();
            first = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            second = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            SEntMan.AddComponent<NarsiCultistComponent>(first);
        });
        await Server.WaitRunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(progress.LeaderEntity, Is.EqualTo(first));
            var leader = SEntMan.GetComponent<NarsiCultistLeaderComponent>(first);
            Assert.That(SEntMan.HasComponent<ActionComponent>(leader.LeaderAbilityEntityUid), Is.True);
            SEntMan.AddComponent<NarsiCultistComponent>(second);
            mobState.ChangeMobState(first, MobState.Dead);
        });
        await Server.WaitRunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(progress.LeaderEntity, Is.EqualTo(second));
            Assert.That(SEntMan.HasComponent<NarsiCultistLeaderComponent>(first), Is.False);
            var leader = SEntMan.GetComponent<NarsiCultistLeaderComponent>(second);
            Assert.That(SEntMan.HasComponent<ActionComponent>(leader.LeaderAbilityEntityUid), Is.True);
            SEntMan.RemoveComponent<NarsiCultistComponent>(second);
        });
        await Server.WaitRunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(progress.LeaderState, Is.EqualTo(LeaderState.NoCandidates));
            Assert.That(SEntMan.HasComponent<NarsiCultistLeaderComponent>(second), Is.False);
            mobState.ChangeMobState(first, MobState.Alive);
        });
        await Server.WaitRunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(progress.LeaderEntity, Is.EqualTo(first));
            SEntMan.DeleteEntity(first);
            SEntMan.AddComponent<NarsiCultistComponent>(second);
        });
        await Server.WaitRunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(progress.LeaderEntity, Is.EqualTo(second));
            Assert.That(SEntMan.HasComponent<NarsiCultistLeaderComponent>(second), Is.True);
        });
    }

    [Test]
    public async Task CultCuffsDisappearWithoutBrokenPrototype()
    {
        var map = await Pair.CreateTestMap();
        var system = SEntMan.System<CuffableSystem>();
        EntityUid cuffs = default;
        EntityUid target = default;
        EntityUid user = default;
        var cuffTime = 0f;
        await Server.WaitAssertion(() =>
        {
            target = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            user = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            cuffs = SEntMan.SpawnEntity("HandcuffsCult", map.GridCoords);
            var component = SEntMan.GetComponent<HandcuffComponent>(cuffs);
            Assert.That(component.BreakOnRemove, Is.True);
            Assert.That(component.BrokenPrototype, Is.Null);
            cuffTime = component.CuffTime;

            Assert.That(SEntMan.System<HandsSystem>().TryPickupAnyHand(user, cuffs), Is.True);
            Assert.That(system.TryCuffing(user, target, cuffs), Is.True);
        });
        await Pair.RunSeconds(cuffTime + 1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<HandcuffComponent>(cuffs).Used, Is.True);
            Assert.That(SEntMan.GetComponent<CuffableComponent>(target).CuffedHandCount, Is.EqualTo(2));
            system.Uncuff(target, user, cuffs);
        });
        await Server.WaitRunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.Deleted(cuffs), Is.True);
            Assert.That(SEntMan.GetComponent<CuffableComponent>(target).CuffedHandCount, Is.Zero);
        });
    }

    [Test]
    public async Task RuneMenuStateRoundTripsUsingCategoryId()
    {
        byte[] payload = null;
        await Server.WaitAssertion(() =>
        {
            var state = new SecretListingInitState(RuneCategory, default);
            using var stream = new MemoryStream();
            Server.ResolveDependency<IRobustSerializer>().Serialize(stream, state);
            payload = stream.ToArray();
        });
        await Client.WaitAssertion(() =>
        {
            using var stream = new MemoryStream(payload);
            var state = Client.ResolveDependency<IRobustSerializer>().Deserialize<SecretListingInitState>(stream);
            Assert.That(state.Category, Is.EqualTo(RuneCategory));
            Assert.That(Client.ProtoMan.Index(state.Category).Listings, Is.Not.Empty);
        });
    }

    [Test]
    public async Task SharedHolyWaterEffectRemovesCultSilence()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var target = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            SEntMan.AddComponent<NarsiSilenceComponent>(target);
            SEntMan.AddComponent<MutedComponent>(target);
            SEntMan.AddComponent<NarsiCultistMuzzleVisualizerComponent>(target);

            Assert.That(SEntMan.System<SharedEntityEffectsSystem>().TryApplyEffect(target, new SaintWaterDrinkEffect()), Is.True);
            Assert.That(SEntMan.HasComponent<NarsiSilenceComponent>(target), Is.False);
            Assert.That(SEntMan.HasComponent<MutedComponent>(target), Is.False);
            Assert.That(SEntMan.HasComponent<NarsiCultistMuzzleVisualizerComponent>(target), Is.False);
        });
    }

    [TestCase(0)]
    [TestCase(45)]
    public async Task BarrierUsesGridTileCenters(int rotation)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var maps = SEntMan.System<SharedMapSystem>();
            for (var x = -5; x <= 5; x++)
            for (var y = -5; y <= 5; y++)
                maps.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, y), map.Tile.Tile);

            var transform = SEntMan.System<SharedTransformSystem>();
            transform.SetLocalRotation(map.Grid, Angle.FromDegrees(rotation));
            var chaplain = SEntMan.SpawnEntity(null, new EntityCoordinates(map.Grid, -0.2f, 0.8f));
            SEntMan.AddComponent<ChaplainComponent>(chaplain);
            var ev = new ChaplainDefenceBarrierEvent { Performer = chaplain };
            SEntMan.EventBus.RaiseLocalEvent(chaplain, ev);
            Assert.That(ev.Handled, Is.True);

            var walls = SEntMan.EntityQuery<MetaDataComponent, TransformComponent>()
                .Where(ent => ent.Item1.EntityPrototype?.ID == "ChaplainForceWallDefault")
                .Select(ent => ent.Item2)
                .ToList();
            Assert.That(walls, Has.Count.EqualTo(12));
            Assert.That(walls.Select(w => w.LocalPosition).Distinct().Count(), Is.EqualTo(12));
            foreach (var wall in walls)
            {
                Assert.That(wall.ParentUid, Is.EqualTo(map.Grid.Owner));
                Assert.That(wall.LocalRotation, Is.EqualTo(Angle.Zero));
                Assert.That(wall.LocalPosition.X % 1f, Is.EqualTo(0.5f).Or.EqualTo(-0.5f));
                Assert.That(wall.LocalPosition.Y % 1f, Is.EqualTo(0.5f).Or.EqualTo(-0.5f));
            }
        });
    }

    [Test]
    public async Task NarsiAnnouncesSpawnWithSound()
    {
        await Server.WaitAssertion(() =>
        {
            var prototype = Server.ProtoMan.Index(NarsiPrototype);
            Assert.That(prototype.TryGetComponent<AnnounceOnSpawnComponent>(out var announcement), Is.True);
            Assert.That(announcement.Sound, Is.TypeOf<SoundPathSpecifier>());
            Assert.That(((SoundPathSpecifier) announcement.Sound).Path.ToString(), Is.EqualTo("/Audio/Misc/narsie_rises.ogg"));
        });
    }
}
