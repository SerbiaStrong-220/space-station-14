// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Photography.UI;
using Content.Shared.SS220.Photography;

namespace Content.Client.SS220.Photography;

public sealed class PhotoBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityMgr = default!;

    private PhotoWindow? _window;
    private readonly PhotoVisualizer _photoVisualizer;
    private PhotoEyeRequest? _photoEyeRequest;

    public PhotoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _photoVisualizer = _entityMgr.System<PhotoVisualizer>();
        _initCallback = new(OnPhotoDataReceived);
        _disposeCallback = new(OnVisualizationDisposed);
    }

    private PhotoEyeRequest.OnVisualizationInitCallback _initCallback;
    private PhotoEyeRequest.OnDisposeCallback _disposeCallback;

    private void OnPhotoDataReceived(EyeComponent eye, float size)
    {
        _window?.SetVisuals(eye, size);
    }

    private void OnVisualizationDisposed()
    {
        _window?.SetVisuals(null, 5);
        _photoEyeRequest = null;
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = new PhotoWindow();
        _window.OnClose += Close;
        _window.ScreenshotComplete += () =>
        {
            _photoEyeRequest?.Dispose();
        };

        if (_entityMgr.TryGetComponent<PhotoComponent>(Owner, out var photo))
        {
            _photoEyeRequest = new(photo.PhotoID)
            {
                OnVisualizationInit = _initCallback,
                OnDispose = _disposeCallback
            };
            _photoVisualizer.RequestPhotoEye(_photoEyeRequest);
        }

        if (_entityMgr.TryGetComponent<MetaDataComponent>(Owner, out var metaData))
        {
            var protoName = metaData.EntityPrototype?.Name;
            var entName = metaData.EntityName;

            // 16.11.2023 there is no client-side label system/component, so using this hack instead
            if (protoName != entName)
            {
                var labelPrefx = protoName + " (";
                var start = entName.IndexOf(labelPrefx);
                if (start != -1) // likely labeled - use whatever is in parentheses
                {
                    start += labelPrefx.Length;
                    var end = entName.IndexOf(")");
                    if (end == -1)
                        end = entName.Length;

                    var finalNameLen = end - start;
                    if (finalNameLen > 0)
                    {
                        var finalName = entName.Substring(start, finalNameLen);
                        _window.SetBackText(finalName);
                    }
                }
                else // not labeled, but still different
                {
                    _window.SetBackText(entName);
                }
            }
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
            _photoEyeRequest?.Dispose();
        }
    }
}
