// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Content.Client.SS220.Photography;
using Content.Client.SS220.Photography.UI;
using Content.IntegrationTests.Fixtures;
using Content.Server.SS220.Photography;
using Content.Shared.SS220.Photography;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Container = Robust.Shared.Containers.Container;

namespace Content.IntegrationTests.Tests.SS220.Photography;

[TestFixture]
[TestOf(typeof(PhotoComponent))]
public sealed class PhotoTest : GameTest
{
    private int _responseCount;
    private long _responseBytes;

    [Test]
    public async Task PhotographFunctions()
    {
        DisposeIsIdempotent();
        DisposeCanBeCalledFromCallback();
        DisposeReleasesVisualizationCallback();
        await ThumbnailAccessRejectsHiddenDistantAndOccludedPhotos();
        await PhotoWindowKeepsOriginalPaperFrameWithoutLegacyStyleClasses();
        await CopiesAndOpenedWindowShareOneDownloadAndTexture(1);
        // These scenarios previously ran separately. Respect the real-time request throttle between them.
        await Task.Delay(TimeSpan.FromSeconds(1.1));
        await CopiesAndOpenedWindowShareOneDownloadAndTexture(20);
    }

    private static void DisposeIsIdempotent()
    {
        var disposeCount = 0;
        var request = new PhotoEyeRequest(EntityUid.Invalid, "photo")
        {
            OnDispose = () => disposeCount++
        };

        request.Dispose();
        request.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(request.Disposed, Is.True);
            Assert.That(disposeCount, Is.EqualTo(1));
            Assert.That(request.OnDispose, Is.Null);
            Assert.That(request.OnVisualizationInit, Is.Null);
        });
    }

    private static void DisposeCanBeCalledFromCallback()
    {
        var request = new PhotoEyeRequest(EntityUid.Invalid, "photo");
        var disposeCount = 0;
        request.OnDispose = () =>
        {
            disposeCount++;
            request.Dispose();
        };

        Assert.DoesNotThrow(request.Dispose);
        Assert.That(disposeCount, Is.EqualTo(1));
    }

    private static void DisposeReleasesVisualizationCallback()
    {
        var request = new PhotoEyeRequest(EntityUid.Invalid, "photo")
        {
            OnVisualizationInit = (_, _) => Assert.Fail("A disposed request must not start a screenshot.")
        };

        request.Dispose();

        Assert.That(request.OnVisualizationInit, Is.Null);
    }

    private async Task ThumbnailAccessRejectsHiddenDistantAndOccludedPhotos()
    {
        var map = await Pair.CreateTestMap();
        try
        {
            EntityUid actor = default;
            EntityUid photo = default;
            EntityUid bag = default;
            await Server.WaitPost(() =>
            {
                actor = SEntMan.SpawnAtPosition("MobHuman", map.GridCoords);
                photo = SEntMan.SpawnAtPosition("Photo", map.GridCoords.Offset(new Vector2(2, 0)));
                bag = SEntMan.SpawnAtPosition("ClothingBackpack", map.GridCoords.Offset(new Vector2(2, 0)));
            });
            await Server.WaitRunTicks(2);
            await Server.WaitAssertion(() =>
            {
                var photos = SEntMan.System<PhotoManager>();
                var transform = SEntMan.System<SharedTransformSystem>();
                Assert.That(photos.CanViewThumbnail(actor, photo), Is.True);
                var container = SEntMan.System<SharedContainerSystem>().EnsureContainer<Container>(bag, "thumbnail-test");
                SEntMan.System<SharedContainerSystem>().Insert(photo, container);
                Assert.That(photos.CanViewThumbnail(actor, photo), Is.False, "A known id must not reveal a photo in a closed bag.");
                SEntMan.System<SharedContainerSystem>().Remove(photo, container);
                transform.SetCoordinates(photo, map.GridCoords.Offset(new Vector2(20, 0)));
                Assert.That(photos.CanViewThumbnail(actor, photo), Is.False);
                transform.SetCoordinates(photo, map.GridCoords.Offset(new Vector2(6, 0)));
                SEntMan.SpawnAtPosition("WallSolid", map.GridCoords.Offset(new Vector2(3, 0)));
            });
            await Server.WaitRunTicks(2);
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.System<PhotoManager>().CanViewThumbnail(actor, photo), Is.False,
                    "A photograph beyond an opaque wall is not a valid preview source.");
            });
        }
        finally
        {
            await Server.WaitPost(() => SEntMan.System<SharedMapSystem>().DeleteMap(map.MapId));
            Pair.TestMap = null;
            await Pair.RunUntilSynced();
        }
    }

    private async Task PhotoWindowKeepsOriginalPaperFrameWithoutLegacyStyleClasses()
    {
        await Client.WaitAssertion(() =>
        {
            using var window = new PhotoWindow();
            var frame = window.FindControl<PanelContainer>("PhotoBackground");
            var background = window.FindControl<PanelContainer>("LoadingBackground");
            Assert.That(frame.PanelOverride, Is.TypeOf<StyleBoxTexture>());
            var border = (StyleBoxTexture) frame.PanelOverride;
            Assert.Multiple(() =>
            {
                Assert.That(border.Texture, Is.Not.Null);
                Assert.That(border.Modulate, Is.EqualTo(Color.White));
                Assert.That(border.GetContentMargin(StyleBox.Margin.Left), Is.EqualTo(16f));
                Assert.That(border.GetContentMargin(StyleBox.Margin.Right), Is.EqualTo(16f));
                Assert.That(border.GetContentMargin(StyleBox.Margin.Top), Is.EqualTo(16f));
                Assert.That(border.GetContentMargin(StyleBox.Margin.Bottom), Is.EqualTo(16f));
                Assert.That(((StyleBoxFlat) background.PanelOverride).BackgroundColor, Is.EqualTo(Color.Black));
            });
        });
    }

    private void OnPhotoData(object sender, object message)
    {
        if (message is not PhotoDataRequestResponse response)
            return;
        if (response.Data == null)
            return;

        _responseCount++;
        using var stream = new MemoryStream();
        Client.ResolveDependency<IRobustSerializer>().Serialize(stream, response);
        _responseBytes = stream.Length;
    }

    private async Task CopiesAndOpenedWindowShareOneDownloadAndTexture(int humanoidCount)
    {
        _responseCount = 0;
        _responseBytes = 0;
        var map = await Pair.CreateTestMap();
        try
        {
            NetEntity first = default;
            NetEntity second = default;
            string photoId = null;
            await Client.WaitPost(() => CEntMan.EntityNetManager.ReceivedSystemMessage += OnPhotoData);
            await Server.WaitPost(() =>
            {
                var actor = SEntMan.SpawnAtPosition("MobHuman", map.GridCoords);
                Server.PlayerMan.SetAttachedEntity(ServerSession, actor);
                for (var i = 1; i < humanoidCount; i++)
                {
                    SEntMan.SpawnAtPosition("MobHuman", map.GridCoords.Offset(new Vector2(i % 4, i / 4)));
                }
                var coordinates = SEntMan.GetComponent<TransformComponent>(actor).MapPosition;
                Assert.That(SEntMan.System<PhotoManager>().TryCapture(coordinates, Angle.Zero, 5, out photoId, out _), Is.True);
                var a = SEntMan.SpawnAtPosition("Photo", map.GridCoords.Offset(new Vector2(1, 0)));
                var b = SEntMan.SpawnAtPosition("Photo", map.GridCoords.Offset(new Vector2(2, 0)));
                SEntMan.GetComponent<PhotoComponent>(a).PhotoID = photoId;
                SEntMan.GetComponent<PhotoComponent>(b).PhotoID = photoId;
                SEntMan.Dirty(a, SEntMan.GetComponent<PhotoComponent>(a));
                SEntMan.Dirty(b, SEntMan.GetComponent<PhotoComponent>(b));
                first = SEntMan.GetNetEntity(a);
                second = SEntMan.GetNetEntity(b);
                var reserved = SEntMan.System<PhotoReservedMap>().EnsureMap();
                SEntMan.EntityNetManager.SendSystemNetworkMessage(new PhotoReservedMapMessage(reserved), ServerSession.Channel);
            });
            await Pair.RunUntilSynced();

            var textures = new List<Texture>();
            var cachedBefore = 0;
            PhotoEyeRequest firstRequest = null;
            PhotoEyeRequest secondRequest = null;
            PhotoEyeRequest windowRequest = null;
            await Client.WaitPost(() =>
            {
                firstRequest = new PhotoEyeRequest(CEntMan.GetEntity(first), photoId) { Thumbnail = true, OnTextureReady = textures.Add };
                secondRequest = new PhotoEyeRequest(CEntMan.GetEntity(second), photoId) { Thumbnail = true, OnTextureReady = textures.Add };
                var visualizer = CEntMan.System<PhotoVisualizer>();
                cachedBefore = visualizer.CachedPhotoTextureCount;
                visualizer.RequestPhotoTexture(firstRequest);
                visualizer.RequestPhotoTexture(secondRequest);
                var pendingBefore = visualizer.PendingPhotoTextureCount;
                var cancelled = new PhotoEyeRequest(CEntMan.GetEntity(second), "cancelled-preview") { Thumbnail = true };
                visualizer.RequestPhotoTexture(cancelled);
                cancelled.Dispose();
                Assert.That(visualizer.PendingPhotoTextureCount, Is.EqualTo(pendingBefore));
            });
            // Headless tests have no screen overlay; explicitly exercise its render-loop entry point.
            for (var i = 0; i < 120 && textures.Count < 2; i++)
            {
                await Pair.RunTicksSync(1);
                await Client.WaitPost(() => CEntMan.System<PhotoVisualizer>().TryRenderThumbnail());
            }

            await Client.WaitAssertion(() =>
            {
                Assert.That(textures, Has.Count.EqualTo(2));
                Assert.That(textures[0], Is.SameAs(textures[1]));
                Assert.That(_responseCount, Is.EqualTo(1));
                // ClydeHeadless returns a 1x1 render target regardless of the requested viewport size.
                Assert.That(textures[0].Size, Is.EqualTo(Vector2i.One));
                Assert.That(CEntMan.System<PhotoVisualizer>().CachedPhotoTextureCount, Is.EqualTo(cachedBefore + 1));
                windowRequest = new PhotoEyeRequest(CEntMan.GetEntity(first), photoId) { OnTextureReady = textures.Add };
                CEntMan.System<PhotoVisualizer>().RequestPhotoTexture(windowRequest);
                Assert.That(textures, Has.Count.EqualTo(3));
                Assert.That(textures[2], Is.SameAs(textures[0]));
                Assert.That(_responseCount, Is.EqualTo(1));
                firstRequest.Dispose();
                secondRequest.Dispose();
                Assert.That(windowRequest.Disposed, Is.False, "Closing an item preview must not invalidate the open window.");
                windowRequest.Dispose();
                var reopen = new PhotoEyeRequest(CEntMan.GetEntity(second), photoId) { Thumbnail = true, OnTextureReady = textures.Add };
                CEntMan.System<PhotoVisualizer>().RequestPhotoTexture(reopen);
                Assert.That(textures, Has.Count.EqualTo(4));
                Assert.That(textures[3], Is.SameAs(textures[0]));
                Assert.That(_responseCount, Is.EqualTo(1));
                reopen.Dispose();
                CEntMan.EntityNetManager.ReceivedSystemMessage -= OnPhotoData;
            });
            TestContext.Out.WriteLine($"PhotoData response, 5x5 frame with {humanoidCount} humanoids: {_responseBytes} bytes.");
        }
        finally
        {
            await Server.WaitPost(() => SEntMan.System<SharedMapSystem>().DeleteMap(map.MapId));
            Pair.TestMap = null;
            await Pair.RunUntilSynced();
        }
    }
}
