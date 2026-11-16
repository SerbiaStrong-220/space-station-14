// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.Examine;
using Content.Shared.SS220.Photography;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Client.SS220.Photography;

public sealed partial class PhotoVisualizer
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    private const int BackgroundTextureLimit = 60;
    private static readonly TimeSpan ThumbnailScanInterval = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan ThumbnailRetryDelay = TimeSpan.FromSeconds(30);
    private TimeSpan _nextThumbnailScan;

    private EntityQuery<SpriteComponent> _thumbnailSprites;
    private EntityQuery<PhotoThumbnailComponent> _thumbnails;

    private void InitializeThumbnails()
    {
        _thumbnailSprites = GetEntityQuery<SpriteComponent>();
        _thumbnails = GetEntityQuery<PhotoThumbnailComponent>();
        SubscribeLocalEvent<PhotoComponent, ComponentStartup>(OnPhotoStartup);
        SubscribeLocalEvent<PhotoComponent, AfterAutoHandleStateEvent>(OnPhotoState);
        SubscribeLocalEvent<PhotoThumbnailComponent, ComponentShutdown>(OnThumbnailShutdown);
    }

    private void OnPhotoStartup(Entity<PhotoComponent> ent, ref ComponentStartup args)
    {
        if (!IsClientSide(ent.Owner))
            EnsureComp<PhotoThumbnailComponent>(ent.Owner);
    }

    private void OnPhotoState(Entity<PhotoComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var thumbnail = EnsureComp<PhotoThumbnailComponent>(ent.Owner);
        if (thumbnail.AppliedId == ent.Comp.PhotoID)
            return;

        thumbnail.Request?.Dispose();
        thumbnail.Request = null;
        thumbnail.AppliedId = ent.Comp.PhotoID;
        thumbnail.RetryAt = TimeSpan.Zero;
    }

    private void OnThumbnailShutdown(Entity<PhotoThumbnailComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Request?.Dispose();
        ent.Comp.Request = null;
        ClearThumbnail(ent.Owner);
    }

    private void UpdateThumbnails()
    {
        if (_timing.RealTime < _nextThumbnailScan)
            return;

        _nextThumbnailScan = _timing.RealTime + ThumbnailScanInterval;
        var viewer = _player.LocalEntity;
        var map = _eyeManager.CurrentEye.Position.MapId;
        var canScan = viewer != null && map != MapId.Nullspace && _eyeManager.MainViewport != null;
        var bounds = canScan ? _eyeManager.GetWorldViewport() : default;
        var newIds = 0;
        var query = EntityQueryEnumerator<PhotoComponent, PhotoThumbnailComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var photo, out var thumbnail, out var sprite, out var xform))
        {
            var visible = canScan && viewer is { } actor && sprite.Visible && xform.MapID == map &&
                bounds.Contains(_transform.GetWorldPosition(xform)) &&
                _containers.IsInSameOrTransparentContainer(actor, uid, userSeeInsideSelf: true);
            if (!visible || photo.PhotoID.Length == 0)
            {
                thumbnail.Request?.Dispose();
                thumbnail.Request = null;
                thumbnail.RetryAt = TimeSpan.Zero;
                continue;
            }

            if (thumbnail.AppliedId != photo.PhotoID)
            {
                thumbnail.Request?.Dispose();
                thumbnail.Request = null;
                thumbnail.AppliedId = photo.PhotoID;
                thumbnail.RetryAt = TimeSpan.Zero;
            }

            if (thumbnail.Request != null || thumbnail.RetryAt > _timing.RealTime)
                continue;

            var subscribed = _textureRequests.ContainsKey(photo.PhotoID);
            if (!subscribed && _textureRequests.Count >= BackgroundTextureLimit)
                continue;

            var known = subscribed || _textureCache.ContainsKey(photo.PhotoID);
            if (!known)
            {
                if (newIds >= 1 ||
                    !_examine.CanExamine(viewer!.Value, uid))
                    continue;
                newIds++;
            }

            var owner = uid;
            var request = new PhotoEyeRequest(owner, photo.PhotoID)
            {
                Thumbnail = true,
                OnTextureReady = texture => ApplyThumbnail(owner, texture),
                OnDispose = () => OnThumbnailReleased(owner)
            };
            thumbnail.Request = request;
            RequestPhotoTexture(request);
        }
    }

    private void ApplyThumbnail(EntityUid uid, Texture texture)
    {
        if (Deleted(uid) || !_thumbnailSprites.TryComp(uid, out var sprite))
            return;

        var layer = _sprite.LayerMapReserve((uid, sprite), PhotoVisualLayers.Image);
        _sprite.LayerSetTexture((uid, sprite), layer, texture);
        _sprite.LayerSetScale((uid, sprite), layer, new Vector2(8f / texture.Width, 8f / texture.Height));
        // The existing 32x32 photo sprite has an 8x8 image opening centered at (13, 16).
        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(-3f / 32f, 0));
        _sprite.LayerSetVisible((uid, sprite), layer, true);
    }

    private void ClearThumbnail(EntityUid uid)
    {
        if (!_thumbnailSprites.TryComp(uid, out var sprite) ||
            !_sprite.LayerMapTryGet((uid, sprite), PhotoVisualLayers.Image, out var layer, false))
            return;

        _sprite.LayerSetVisible((uid, sprite), layer, false);
        _sprite.LayerSetTexture((uid, sprite), layer, (Texture?) null);
    }

    private void OnThumbnailReleased(EntityUid uid)
    {
        ClearThumbnail(uid);
        if (!_thumbnails.TryComp(uid, out var thumbnail))
            return;

        thumbnail.Request = null;
        thumbnail.RetryAt = _timing.RealTime + ThumbnailRetryDelay;
    }

}
