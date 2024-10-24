// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Overlays
{
    /// <summary>
    /// Special overlays container created to bypass limits of <see cref="IOverlayManager"/> not supporting multiple overlays of the same type.
    /// </summary>
    public sealed class OverlayStack : Overlay
    {
        public override OverlaySpace Space => _space;
        private readonly SortedDictionary<StackableOverlay, OverlayData> _overlays = new(OverlayComparer.Instance);

        private OverlaySpace _space;

        public bool AddOverlay(StackableOverlay overlay)
        {
            if (_overlays.ContainsKey(overlay))
                return false;
            _overlays.Add(overlay, new());
            _space |= overlay.Space;
            return true;
        }

        public bool RemoveOverlay(StackableOverlay overlay)
        {
            if (!_overlays.ContainsKey(overlay))
                return false;
            _overlays.Remove(overlay);
            return true;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            foreach (var (overlay, _) in _overlays)
            {
                overlay.FrameUpdatePublic(args);
            }
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            var result = false;
            foreach (var (overlay, data) in _overlays)
            {
                if (!ShouldDraw(overlay, args))
                    continue;
                var shouldDraw = overlay.BeforeDrawPublic(in args);
                data.ShouldDrawThisFrame = shouldDraw;
                result |= shouldDraw;
            }
            return result;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            foreach (var (overlay, data) in _overlays)
            {
                if (!ShouldDraw(overlay, args) || !data.ShouldDrawThisFrame)
                    continue;
                overlay.DrawPublic(in args);
            }
        }

        public static OverlayStack Get(IOverlayManager overlayManager)
        {
            if (overlayManager.TryGetOverlay<OverlayStack>(out var overlay))
                return overlay;
            overlay = new OverlayStack();
            overlayManager.AddOverlay(overlay);
            return overlay;
        }

        private static bool ShouldDraw(StackableOverlay overlay, OverlayDrawArgs drawArgs)
        {
            return (overlay.Space & drawArgs.Space) != 0;
        }

        private sealed class OverlayData
        {
            public bool ShouldDrawThisFrame;
        }

        private sealed class OverlayComparer : IComparer<Overlay>
        {
            public static readonly OverlayComparer Instance = new();

            public int Compare(Overlay? x, Overlay? y)
            {
                var zX = x?.ZIndex ?? 0;
                var zY = y?.ZIndex ?? 0;
                return zX.CompareTo(zY);
            }
        }
    }

    /// <summary>
    /// An overlay supported by <see cref="OverlayStack"/>
    /// </summary>
    public abstract class StackableOverlay : Overlay
    {
        public void FrameUpdatePublic(FrameEventArgs args)
        {
            FrameUpdate(args);
        }

        public bool BeforeDrawPublic(in OverlayDrawArgs args)
        {
            return BeforeDraw(in args);
        }

        public void DrawPublic(in OverlayDrawArgs args)
        {
            Draw(in args);
        }
    }
}
