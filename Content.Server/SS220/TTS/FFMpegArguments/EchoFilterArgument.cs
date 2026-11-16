// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Globalization;
using System.Linq;
using System.Text;
using FFMpegCore.Arguments;

namespace Content.Server.SS220.TTS.FFMPegArguments;

public sealed class EchoFilterArgument : IAudioFilterArgument
{
    private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();

    public EchoFilterArgument(
        double inGain = 1d,
        double outGain = 1d,
        // in milliseconds between 0 and 90000.00 ms -> 90 s
        IEnumerable<double>? delays = null,
        // between 0 and 1
        IEnumerable<double>? decays = null)
    {
        _arguments.Add("in_gain", inGain.ToString("n2", CultureInfo.InvariantCulture));
        _arguments.Add("out_gain", outGain.ToString("n2", CultureInfo.InvariantCulture));

        var internalDelays = delays ?? [100d, 220d, 370d];
        var internalDecays = decays ?? [0.2d, 0.12d, 0.06d];

        var builder = new StringBuilder();
        builder.AppendJoin('|', internalDelays.Select(x => x.ToString("n2", CultureInfo.InvariantCulture)));
        _arguments.Add("delays", builder.ToString());

        builder.Clear();
        builder.AppendJoin('|', internalDecays.Select(x => x.ToString("n2", CultureInfo.InvariantCulture)));
        _arguments.Add("decays", builder.ToString());
    }

    public string Key => "aecho";

    public string Value => string.Join(":", _arguments.Select(pair => pair.Key + "=" + pair.Value));
}
