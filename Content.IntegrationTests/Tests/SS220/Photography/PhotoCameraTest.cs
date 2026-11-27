// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.SS220.Photography;
using Content.Shared.Body;
using Content.Shared.Charges.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Roles;
using Content.Shared.SS220.Photography;
using Content.Shared.Timing;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests.SS220.Photography;

[TestFixture]
[TestOf(typeof(PhotoCameraSystem))]
public sealed class PhotoCameraTest : GameTest
{
    [Test]
    public async Task CameraFunctions()
    {
        await ReporterGearYamlValidatesAndEquipsCameraInPockets();
        await CaptureHumanoidWithVisualOrgans();
        await CaptureAtCursor(6f, obstructed: false, succeeds: true);
        await CaptureAtCursor(11f, obstructed: false, succeeds: false);
        await CaptureAtCursor(6f, obstructed: true, succeeds: false);
        await CaptureAtCursor(float.NaN, obstructed: false, succeeds: false);
    }

    private async Task ReporterGearYamlValidatesAndEquipsCameraInPockets()
    {
        await Server.WaitAssertion(() =>
        {
            var resources = Server.ResolveDependency<IResourceManager>();
            var serialization = Server.ResolveDependency<ISerializationManager>();
            using var reader = resources.ContentFileReadText(new ResPath("/Prototypes/Roles/Jobs/Wildcards/reporter.yml"));
            var yaml = new YamlStream();
            yaml.Load(reader);
            var prototypes = (YamlSequenceNode) yaml.Documents[0].RootNode;
            var reporter = prototypes.Children.Cast<YamlMappingNode>()
                .Single(node => node.GetNode("id").AsString() == "ReporterGear");
            var data = reporter.ToDataNodeCast<MappingDataNode>();
            data.Remove("type");

            // Validate the raw YAML, not a deserialized prototype which could hide an invalid node shape.
            Assert.That(serialization.ValidateNode<StartingGearPrototype>(data).GetErrors(), Is.Empty);
            var gear = serialization.Read<StartingGearPrototype>(data, notNullableOverride: true);
            Assert.Multiple(() =>
            {
                Assert.That(gear.Equipment["pocket1"].Id, Is.EqualTo("PhotoCamera"));
                Assert.That(gear.Equipment["pocket2"].Id, Is.EqualTo("PhotoCameraFilm"));
                Assert.That(gear.Storage.ContainsKey("pocket1"), Is.False);
                Assert.That(gear.Storage.ContainsKey("pocket2"), Is.False);
                Assert.That(gear.Storage["back"].Select(id => id.Id),
                    Is.EqualTo(new[] { "TapeRecorder", "CassetteTape", "CassetteTape" }));
            });
        });
    }

    private async Task CaptureHumanoidWithVisualOrgans()
    {
        var map = await Pair.CreateTestMap();
        try
        {
            EntityUid humanoid = default;

            await Server.WaitPost(() =>
            {
                humanoid = SEntMan.SpawnAtPosition("MobHuman", map.GridCoords);
            });

            await Server.WaitRunTicks(2);

            await Server.WaitAssertion(() =>
            {
                var body = SEntMan.GetComponent<BodyComponent>(humanoid);
                Assert.That(body.Organs, Is.Not.Null);
                Assert.That(body.Organs.ContainedEntities, Is.Not.Empty);

                var photos = SEntMan.System<PhotoManager>();
                var coordinates = SEntMan.GetComponent<TransformComponent>(humanoid).MapPosition;
                Assert.That(photos.TryCapture(coordinates, Angle.Zero, 5, out var id, out var seen), Is.True);
                Assert.That(id, Is.Not.Empty);
                Assert.That(seen, Does.Contain(Identity.Name(humanoid, SEntMan)));
            });
        }
        finally
        {
            await Server.WaitPost(() => SEntMan.System<SharedMapSystem>().DeleteMap(map.MapId));
            Pair.TestMap = null;
            await Pair.RunUntilSynced();
        }
    }

    private async Task CaptureAtCursor(float distance, bool obstructed, bool succeeds)
    {
        var map = await Pair.CreateTestMap();
        try
        {
            EntityUid photographer = default;
            EntityUid camera = default;
            EntityCoordinates focus = default;

            await Server.WaitPost(() =>
            {
                photographer = SEntMan.SpawnAtPosition("MobHuman", map.GridCoords);
                camera = SEntMan.SpawnAtPosition("PhotoCamera", map.GridCoords);
                focus = map.GridCoords.Offset(new Vector2(distance, 0));
                var subject = SEntMan.SpawnAtPosition("MobHuman", map.GridCoords.Offset(new Vector2(6, 0)));
                var metadata = SEntMan.System<MetaDataSystem>();
                metadata.SetEntityName(photographer, "Photographer");
                metadata.SetEntityName(subject, "Subject");
                SEntMan.GetComponent<PhotoCameraComponent>(camera).SelectedPhotoDimensions = 3;

                if (obstructed)
                    SEntMan.SpawnAtPosition("WallSolid", map.GridCoords.Offset(new Vector2(3, 0)));
            });

            await Server.WaitRunTicks(2);

            await Server.WaitAssertion(() =>
            {
                var charges = SEntMan.System<SharedChargesSystem>();
                var chargesBefore = charges.GetCurrentCharges(camera);
                var interaction = SEntMan.System<SharedInteractionSystem>();
                // CanReach is false: camera focus must not be limited to ordinary hand interaction range.
                Assert.That(interaction.RangedInteractDoBefore(photographer, camera, null, focus, canReach: false), Is.True);
                Assert.That(charges.GetCurrentCharges(camera), Is.EqualTo(chargesBefore - (succeeds ? 1 : 0)));
                Assert.That(SEntMan.System<UseDelaySystem>().IsDelayed(camera), Is.EqualTo(succeeds));

                var count = 0;
                var query = SEntMan.AllEntityQueryEnumerator<PhotoComponent>();
                var transform = SEntMan.System<SharedTransformSystem>();
                while (query.MoveNext(out var uid, out var photo))
                {
                    if (transform.GetMapCoordinates(uid).MapId != map.MapId)
                        continue;

                    count++;
                    Assert.That(photo.SeenObjects, Does.Contain("Subject"));
                    Assert.That(photo.SeenObjects, Does.Not.Contain("Photographer"));
                    // The focus moves to the cursor, but the physical print stays with the photographer.
                    Assert.That(transform.GetWorldPosition(uid), Is.EqualTo(transform.GetWorldPosition(photographer)));
                }

                Assert.That(count, Is.EqualTo(succeeds ? 1 : 0));
                if (succeeds)
                {
                    // A second click in the same tick must not consume film or create another snapshot.
                    interaction.RangedInteractDoBefore(photographer, camera, null, focus, canReach: false);
                    Assert.That(charges.GetCurrentCharges(camera), Is.EqualTo(chargesBefore - 1));
                }
            });
        }
        finally
        {
            await Server.WaitPost(() => SEntMan.System<SharedMapSystem>().DeleteMap(map.MapId));
            Pair.TestMap = null;
            await Pair.RunUntilSynced();
        }
    }
}
