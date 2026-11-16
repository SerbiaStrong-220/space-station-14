// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.SS220.Photography;
using Content.Shared.Charges.Systems;
using Content.Shared.Interaction;
using Content.Shared.SS220.Photography;
using Content.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.SS220.Photography;

[TestFixture]
[TestOf(typeof(PhotoCameraSystem))]
public sealed class PhotoCameraCursorTest : GameTest
{
    [TestCase(6f, false, true)]
    [TestCase(11f, false, false)]
    [TestCase(6f, true, false)]
    [TestCase(float.NaN, false, false)]
    public async Task CaptureAtCursor(float distance, bool obstructed, bool succeeds)
    {
        var map = await Pair.CreateTestMap();
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
}
