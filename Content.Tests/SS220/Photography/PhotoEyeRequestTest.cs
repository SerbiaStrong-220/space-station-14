// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Photography;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.Tests.SS220.Photography;

[TestFixture]
public sealed class PhotoEyeRequestTest
{
    [Test]
    public void DisposeIsIdempotent()
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

    [Test]
    public void DisposeCanBeCalledFromCallback()
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

    [Test]
    public void DisposeReleasesVisualizationCallback()
    {
        var request = new PhotoEyeRequest(EntityUid.Invalid, "photo")
        {
            OnVisualizationInit = (_, _) => Assert.Fail("A disposed request must not start a screenshot.")
        };

        request.Dispose();

        Assert.That(request.OnVisualizationInit, Is.Null);
    }
}
