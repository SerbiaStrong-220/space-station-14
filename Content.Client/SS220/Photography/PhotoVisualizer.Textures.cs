// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Content.Client.SS220.Photography;

public sealed partial class PhotoVisualizer
{
    [Dependency] private IClyde _display = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;

    private const int TextureCacheLimit = 64;
    private const int MaxTextureSize = 256;
    private readonly Dictionary<string, (OwnedTexture Texture, LinkedListNode<string> Node)> _textureCache = new();
    private readonly LinkedList<string> _textureLru = new();
    private readonly LinkedList<string> _pendingTextures = new();
    private readonly Dictionary<string, HashSet<PhotoEyeRequest>> _textureRequests = new();
    private PhotoEyeRequest? _textureJob;
    private IClydeViewport? _textureViewport;
    private int _textureGeneration;

    [ViewVariables] public int CachedPhotoTextureCount => _textureCache.Count;
    [ViewVariables] public long CachedPhotoTextureBytes { get; private set; }
    [ViewVariables] public int PendingPhotoTextureCount => _pendingTextures.Count;

    private void InitializeTextures()
    {
        _overlayManager.AddOverlay(new PhotoThumbnailOverlay(this));
    }

    /// <summary>
    /// Requests a shared, immutable client texture. Keep the request alive while using the texture.
    /// </summary>
    public void RequestPhotoTexture(PhotoEyeRequest request)
    {
        if (request.Disposed || request.Cleanup != null)
            return;

        if (!_textureRequests.TryGetValue(request.PhotoId, out var requests))
        {
            requests = new HashSet<PhotoEyeRequest>();
            _textureRequests.Add(request.PhotoId, requests);
            if (request.Thumbnail)
                _pendingTextures.AddLast(request.PhotoId);
            else
                _pendingTextures.AddFirst(request.PhotoId);
        }

        requests.Add(request);
        if (!request.Thumbnail && _pendingTextures.Find(request.PhotoId) is { } pending)
        {
            _pendingTextures.Remove(pending);
            _pendingTextures.AddFirst(pending);
        }
        request.Cleanup = ForgetTextureRequest;
        if (_textureCache.TryGetValue(request.PhotoId, out var cached))
        {
            _textureLru.Remove(cached.Node);
            _textureLru.AddLast(cached.Node);
            request.OnTextureReady?.Invoke(cached.Texture);
        }
    }

    private void ForgetTextureRequest(PhotoEyeRequest request)
    {
        if (!_textureRequests.TryGetValue(request.PhotoId, out var requests))
            return;

        requests.Remove(request);
        if (requests.Count != 0)
            return;

        _textureRequests.Remove(request.PhotoId);
        if (_pendingTextures.Find(request.PhotoId) is { } pending)
            _pendingTextures.Remove(pending);
        if (_textureJob?.PhotoId == request.PhotoId)
        {
            var job = _textureJob;
            _textureJob = null;
            CancelTextureViewport();
            job.Dispose();
        }
    }

    private bool TryMakeTextureRoom()
    {
        if (_textureCache.Count < TextureCacheLimit)
            return true;

        var node = _textureLru.First;
        while (node != null)
        {
            var next = node.Next;
            if (!_textureRequests.ContainsKey(node.Value))
            {
                _textureCache.Remove(node.Value, out var cached);
                _textureLru.Remove(node);
                CachedPhotoTextureBytes -= (long) cached.Texture.Width * cached.Texture.Height * 4;
                cached.Texture.Dispose();
                return true;
            }
            node = next;
        }
        return false; // All slots are in use: leave remaining photos on their placeholder, without more traffic.
    }

    private void UpdateTextureQueue()
    {
        if (_textureJob != null)
            return;

        var start = Stopwatch.GetTimestamp();
        while (_pendingTextures.First is { } node && Stopwatch.GetElapsedTime(start) < VisualizationBudget)
        {
            var id = node.Value;
            if (!_textureRequests.TryGetValue(id, out var requests) || _textureCache.ContainsKey(id))
            {
                _pendingTextures.RemoveFirst();
                continue;
            }

            if (!TryMakeTextureRoom())
                return;

            PhotoEyeRequest? source = null;
            foreach (var request in requests)
            {
                if (source == null || !request.Thumbnail)
                    source = request;
                if (!request.Thumbnail)
                    break;
            }

            _pendingTextures.RemoveFirst();
            if (source == null)
                continue;

            var job = new PhotoEyeRequest(source.Owner, id) { Thumbnail = source.Thumbnail };
            job.OnVisualizationInit = (eye, size) => QueueTextureCapture(job, eye, size);
            job.OnDispose = () => OnTextureJobFailed(job);
            _textureJob = job;
            RequestPhotoEye(job);
            return;
        }
    }

    private void QueueTextureCapture(PhotoEyeRequest job, EyeComponent eye, float size)
    {
        var pixels = Math.Clamp((int) MathF.Ceiling(size * EyeManager.PixelsPerMeter), 1, MaxTextureSize);
        _textureViewport = _display.CreateViewport(new Vector2i(pixels, pixels),
            new TextureSampleParameters { Filter = false }, "Photo thumbnail");
        _textureViewport.Eye = eye.Eye;
        _textureViewport.RenderScale = new Vector2(pixels / (size * EyeManager.PixelsPerMeter));
    }

    /// <summary>
    /// Called by the screen overlay inside Clyde's render loop. No per-frame scene re-rendering.
    /// </summary>
    public bool TryRenderThumbnail()
    {
        if (_textureViewport is not { } viewport || _textureJob is not { } job)
            return false;

        _textureViewport = null;
        var generation = _textureGeneration;
        try
        {
            viewport.Render();
        }
        catch
        {
            viewport.Dispose();
            job.Dispose();
            throw;
        }
        viewport.RenderTarget.CopyPixelsToMemory<Rgba32>(image =>
        {
            using (image)
            {
                try
                {
                    if (generation == _textureGeneration && _textureJob == job && !job.Disposed)
                        CompleteTexture(job, image);
                }
                finally
                {
                    viewport.Dispose();
                }
            }
        });
        return true;
    }

    private void CompleteTexture(PhotoEyeRequest job, Image<Rgba32> image)
    {
        var texture = _display.LoadTextureFromImage(image, loadParams: new TextureLoadParameters
        {
            Srgb = true,
            SampleParameters = new TextureSampleParameters { Filter = false }
        });
        var node = _textureLru.AddLast(job.PhotoId);
        _textureCache.Add(job.PhotoId, (texture, node));
        CachedPhotoTextureBytes += (long) texture.Width * texture.Height * 4;
        _textureJob = null;
        job.Dispose();
        if (!_textureRequests.TryGetValue(job.PhotoId, out var requests))
            return;

        // Callbacks can close windows and remove themselves from this set.
        foreach (var request in new List<PhotoEyeRequest>(requests))
        {
            if (!request.Disposed)
                request.OnTextureReady?.Invoke(texture);
        }
    }

    private void OnTextureJobFailed(PhotoEyeRequest job)
    {
        if (_textureJob != job)
            return;

        _textureJob = null;
        CancelTextureViewport();
        if (!_textureRequests.Remove(job.PhotoId, out var requests))
            return;

        foreach (var request in requests)
        {
            request.Dispose();
        }
    }

    private void CancelTextureViewport()
    {
        _textureGeneration++;
        _textureViewport?.Dispose();
        _textureViewport = null;
    }

    private void CleanupThumbnails()
    {
        CancelTextureViewport();
        _textureJob = null;
        foreach (var requests in _textureRequests.Values)
        {
            foreach (var request in requests)
            {
                request.Cleanup = null;
                request.Dispose();
            }
        }
        _textureRequests.Clear();
        _pendingTextures.Clear();
        foreach (var cached in _textureCache.Values)
        {
            cached.Texture.Dispose();
        }
        _textureCache.Clear();
        CachedPhotoTextureBytes = 0;
        _textureLru.Clear();
        _nextThumbnailScan = TimeSpan.Zero;
    }
}
