using System.Numerics;
using Content.Shared.SS220.Signature;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Threading;

namespace Content.Client.SS220.Signature;

public sealed class SignatureControl : Control
{
    [Dependency] private readonly IClyde _clyde = default!;

    public Vector2 CanvasSize { get; set; }

    public SignatureData? Data;

    public int BrushWriteSize = 1;
    public int BrushEraseSize = 2;

    public bool Editable { get; set; } = true;

    public Color BackgroundColor { get; set; } = Color.FromHex("#ffffff88");

    public Color BorderColor { get; set; } = Color.Black.WithAlpha(0.4f);

    public event Action<SignatureData?>? SignatureChanged;

    private SignatureDrawMode _currentMode = SignatureDrawMode.Write;
    private bool _isDrawing;

    private (int x, int y)? _lastPixel;

    private IRenderTexture? _canvas;
    private bool _dirty;

    public SignatureControl()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Resized()
    {
        base.Resized();

        if (CanvasSize == Vector2.Zero)
            return;

        _dirty = true;
        _canvas?.Dispose();
        _canvas = null;
    }

    public void SetSignature(SignatureData? data)
    {
        if (CanvasSize == Vector2.Zero)
        {
            Data = data ?? new SignatureData(1, 1);
            return;
        }

        Data = data == null
            ? new SignatureData((int)CanvasSize.X, (int)CanvasSize.Y)
            : EnsureSize(data);

        _dirty = true;
        _canvas?.Dispose();
        _canvas = null;
    }

    private SignatureData EnsureSize(SignatureData original)
    {
        if (original.Width == (int)CanvasSize.X &&
            original.Height == (int)CanvasSize.Y)
            return original;

        var clone = new SignatureData((int)CanvasSize.X, (int)CanvasSize.Y);

        for (var y = 0; y < Math.Min(original.Height, clone.Height); y++)
        {
            for (var x = 0; x < Math.Min(original.Width, clone.Width); x++)
            {
                if (original.GetPixel(x, y))
                    clone.SetPixel(x, y);
            }
        }

        return clone;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!Editable)
            return;

        Data ??= new SignatureData((int)CanvasSize.X, (int)CanvasSize.Y);

        if (args.Function == EngineKeyFunctions.UIClick)
            _currentMode = SignatureDrawMode.Write;
        else if (args.Function == EngineKeyFunctions.UIRightClick)
            _currentMode = SignatureDrawMode.Erase;
        else
            return;

        if (!TryLocalToPixel(args.RelativePixelPosition, out var px, out var py))
            return;

        _isDrawing = true;
        DrawPixel(px, py);
        _lastPixel = (px, py);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (!Editable ||
            args.Function != EngineKeyFunctions.UIClick &&
            args.Function != EngineKeyFunctions.UIRightClick)
            return;

        if (!_isDrawing)
            return;

        _isDrawing = false;
        _lastPixel = null;

        SignatureChanged?.Invoke(Data);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!Editable || !_isDrawing)
            return;

        if (!TryLocalToPixel(args.RelativePixelPosition, out var px, out var py))
            return;

        if (_lastPixel is { } last)
            DrawLine(last.x, last.y, px, py);
        else
            DrawPixel(px, py);

        _lastPixel = (px, py);
    }

    private bool TryLocalToPixel(Vector2 local, out int px, out int py)
    {
        var size = PixelSize;
        if (size.X <= 0 || size.Y <= 0)
        {
            px = py = 0;
            return false;
        }

        var nx = Math.Clamp(local.X / size.X, 0f, 0.999999f);
        var ny = Math.Clamp(local.Y / size.Y, 0f, 0.999999f);

        px = (int)(nx * CanvasSize.X);
        py = (int)(ny * CanvasSize.Y);
        return true;
    }

    private void DrawPixel(int x, int y)
    {
        if (Data == null)
            return;

        var radius = _currentMode == SignatureDrawMode.Erase ? BrushEraseSize : BrushWriteSize;
        var half = radius / 2;

        for (var yy = -half; yy <= half; yy++)
        {
            for (var xx = -half; xx <= half; xx++)
            {
                var px = x + xx;
                var py = y + yy;

                if (px < 0 || px >= Data.Width || py < 0 || py >= Data.Height)
                    continue;

                switch (_currentMode)
                {
                    case SignatureDrawMode.Write:
                        Data.SetPixel(px, py);
                        break;
                    case SignatureDrawMode.Erase:
                        Data.ErasePixel(px, py);
                        break;
                }

                _dirty = true;
            }
        }
    }

    private void DrawLine(int x0, int y0, int x1, int y1)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            DrawPixel(x0, y0);

            if (x0 == x1 && y0 == y1)
                break;

            var e2 = err * 2;

            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void ClearSignature()
    {
        if (Data == null)
            return;

        Data.Clear();
        _dirty = true;
        SignatureChanged?.Invoke(Data);
        InvalidateMeasure();
    }

    private void UpdateCanvas(DrawingHandleScreen handle)
    {
        if (CanvasSize.X <= 0 || CanvasSize.Y <= 0)
            return;

        if (Data == null)
            return;

        if (_canvas == null)
        {
            _canvas = _clyde.CreateRenderTarget(
                new Vector2i((int)CanvasSize.X, (int)CanvasSize.Y),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "signature");
            _dirty = true;
        }

        if (!_dirty)
            return;

        handle.RenderInRenderTarget(_canvas,
            () =>
            {
                for (var y = 0; y < Data.Height; y++)
                {
                    for (var x = 0; x < Data.Width; x++)
                    {
                        if (!Data.GetPixel(x, y))
                            continue;

                        var r = new UIBox2i(x, y, x + 1, y + 1);
                        handle.DrawRect(r, Color.Black);
                    }
                }
            },
            Color.Transparent);

        _dirty = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var rect = new UIBox2(0, 0, size.X, size.Y);

        if (Editable)
        {
            handle.DrawRect(rect, BackgroundColor);
            handle.DrawRect(rect, BorderColor, false);
        }

        UpdateCanvas(handle);

        if (_canvas != null)
            handle.DrawTextureRect(_canvas.Texture, rect);
    }
}

public enum SignatureDrawMode
{
    Write,
    Erase,
}
