// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Shared.SS220.Virology;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Virology;

public sealed partial class DnaProgressBar : Control
{
    private static readonly ProtoId<ShaderPrototype> Shader = "DnaProgressBar";

    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly ShaderInstance _shader;

    public float Progress;

    private VirologyMachineStatus _status;
    private TimeSpan? _operationEnd;
    private TimeSpan _operationDuration;

    public Color StrandAColor = Color.FromHex("#2C5AD8");
    public Color StrandADimColor = Color.FromHex("#1B2C55");
    public Color StrandBColor = Color.FromHex("#D03636");
    public Color StrandBDimColor = Color.FromHex("#4C1E1E");
    public Color BackgroundColor = Color.Transparent;

    public float WaveScale = 3f;
    public float RungsPerWave = 4f;
    public float RungInset = 0.25f;
    public float RungPeakBias = 0.2f;
    public float Amplitude = 0.32f;

    public DnaProgressBar()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(Shader).InstanceUnique();
        MinSize = new Vector2(0, 38);
    }

    public void SetOperation(VirologyMachineStatus status, TimeSpan? operationEnd, TimeSpan operationDuration)
    {
        _status = status;
        _operationEnd = operationEnd;
        _operationDuration = operationDuration;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        Progress = ComputeProgress();
    }

    private float ComputeProgress()
    {
        switch (_status)
        {
            case VirologyMachineStatus.Scanning when _operationEnd is { } end && _operationDuration > TimeSpan.Zero:
            {
                var remaining = (end - _timing.CurTime) / _operationDuration;
                return Math.Clamp(1f - (float)remaining, 0f, 1f);
            }
            case VirologyMachineStatus.Printing:
            case VirologyMachineStatus.Result:
                return 1f;
            default:
                return 0f;
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var w = PixelWidth;
        var h = PixelHeight;
        if (w <= 2f || h <= 2f)
            return;

        _shader.SetParameter("progress", Math.Clamp(Progress, 0f, 1f));
        _shader.SetParameter("wave_scale", WaveScale);
        _shader.SetParameter("rungs_per_wave", RungsPerWave);
        _shader.SetParameter("rung_inset", RungInset);
        _shader.SetParameter("rung_peak_bias", RungPeakBias);
        _shader.SetParameter("amp", Amplitude);
        _shader.SetParameter("scale", UIScale);
        _shader.SetParameter("rect_size", new Vector2(w, h));
        _shader.SetParameter("strand_a", StrandAColor);
        _shader.SetParameter("strand_a_dim", StrandADimColor);
        _shader.SetParameter("strand_b", StrandBColor);
        _shader.SetParameter("strand_b_dim", StrandBDimColor);
        _shader.SetParameter("background", BackgroundColor);

        handle.UseShader(_shader);
        handle.DrawRect(PixelSizeBox, Color.White);
        handle.UseShader(null);
    }
}
