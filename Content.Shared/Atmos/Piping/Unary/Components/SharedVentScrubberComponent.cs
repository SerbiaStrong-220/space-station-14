using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    [Serializable, NetSerializable]
    public sealed class GasVentScrubberData : IAtmosDeviceData
    {
        public bool Enabled { get; set; }
        public bool Dirty { get; set; }
        public bool IgnoreAlarms { get; set; } = false;
        public HashSet<Gas> FilterGases { get; set; } = new(DefaultFilterGases);
        public ScrubberPumpDirection PumpDirection { get; set; } = ScrubberPumpDirection.Scrubbing;
        public float VolumeRate { get; set; } = 200f;
        public bool WideNet { get; set; } = false;

        //SS220-AirAlarm start
        public static HashSet<Gas> DefaultFilterGases = new()
        {
            Gas.CarbonDioxide
        };

        public static HashSet<Gas> DefaultBasaFilterGases = new()
        {
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
            Gas.Ammonia,
            Gas.NitrousOxide,
            Gas.Frezon,
        };

        public static HashSet<Gas> DefaultFullFilterGases = new()
        {
        };

        public static HashSet<Gas> DefaultPanicFilterGases = new()
        {
            Gas.Oxygen,
            Gas.Nitrogen,
            Gas.CarbonDioxide,
            Gas.Plasma,
            Gas.Tritium,
            Gas.WaterVapor,
            Gas.Ammonia,
            Gas.NitrousOxide,
            Gas.Frezon
        };
        //SS220-AirAlarm end

        //SS220-scrubber-mode begin
        [NetSerializable]
        public enum ScrubberFilter
        {
            Default,
            Basa,
            Full,
            Panic
        }

        public static readonly Dictionary<ScrubberFilter, HashSet<Gas>> ScrubberFilters = new()
        {
            { ScrubberFilter.Default, DefaultFilterGases },
            { ScrubberFilter.Basa, DefaultBasaFilterGases },
            { ScrubberFilter.Full, DefaultFullFilterGases },
            { ScrubberFilter.Panic, DefaultPanicFilterGases }
        };
        //SS220-scrubber-mode end

        // Presets for 'dumb' air alarm modes

        public static GasVentScrubberData FilterModePreset = new GasVentScrubberData
        {
            Enabled = true,
            FilterGases = new(GasVentScrubberData.DefaultFilterGases), //SS220-AirAlarm
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = false
        };

        public static GasVentScrubberData WideFilterModePreset = new GasVentScrubberData
        {
            Enabled = true,
            FilterGases = new(GasVentScrubberData.DefaultBasaFilterGases), //SS220-AirAlarm
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = true
        };

        public static GasVentScrubberData FillModePreset = new GasVentScrubberData
        {
            Enabled = false,
            Dirty = true,
            FilterGases = new(GasVentScrubberData.DefaultFullFilterGases), //SS220-AirAlarm
            PumpDirection = ScrubberPumpDirection.Scrubbing,
            VolumeRate = 200f,
            WideNet = false
        };

        public static GasVentScrubberData PanicModePreset = new GasVentScrubberData
        {
            Enabled = true,
            Dirty = true,
            FilterGases = new(GasVentScrubberData.DefaultPanicFilterGases), //SS220-AirAlarm
            PumpDirection = ScrubberPumpDirection.Siphoning,
            VolumeRate = 200f,
            WideNet = true //SS220-AirAlarm
        };

        public static GasVentScrubberData ReplaceModePreset = new GasVentScrubberData
        {
            Enabled = true,
            IgnoreAlarms = true,
            Dirty = true,
            FilterGases = new(GasVentScrubberData.DefaultFilterGases),
            PumpDirection = ScrubberPumpDirection.Siphoning,
            VolumeRate = 200f,
            WideNet = false
        };
    }

    [Serializable, NetSerializable]
    public enum ScrubberPumpDirection : sbyte
    {
        Siphoning = 0,
        Scrubbing = 1,
    }
}
