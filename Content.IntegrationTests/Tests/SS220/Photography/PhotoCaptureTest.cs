// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.IntegrationTests.Fixtures;
using Content.Server.SS220.Photography;
using Content.Shared.Body;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.SS220.Photography;

[TestFixture]
[TestOf(typeof(PhotoManager))]
public sealed class PhotoCaptureTest : GameTest
{
    [Test]
    public async Task CaptureHumanoidWithVisualOrgans()
    {
        var map = await Pair.CreateTestMap();
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
}
