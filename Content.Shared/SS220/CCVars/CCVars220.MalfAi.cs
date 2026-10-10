// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    public static readonly CVarDef<bool> MalfAiEnabled =
        CVarDef.Create("malfai.enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<int> MalfAiApcReward =
        CVarDef.Create("malfai.apc_reward", 1, CVar.SERVERONLY);

    public static readonly CVarDef<int> MalfAiApcRewardInterval =
        CVarDef.Create("malfai.apc_reward_interval", 60, CVar.SERVERONLY);

    public static readonly CVarDef<int> MalfAiStartingCpu =
        CVarDef.Create("malfai.starting_cpu", 0, CVar.SERVERONLY);

    public static readonly CVarDef<int> MalfAiMaxTurrets =
        CVarDef.Create("malfai.max_turrets", 8, CVar.SERVERONLY);

    public static readonly CVarDef<int> MalfAiMaxOverrides =
        CVarDef.Create("malfai.max_overrides", 5, CVar.SERVERONLY);

    public static readonly CVarDef<int> MalfAiMaxFactories =
        CVarDef.Create("malfai.max_factories", 1, CVar.SERVERONLY);

    public static readonly CVarDef<int> MalfAiMaxSiphons =
        CVarDef.Create("malfai.max_siphons", 1, CVar.SERVERONLY);

    public static readonly CVarDef<bool> MalfAiLockdownEnabled =
        CVarDef.Create("malfai.lockdown_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> MalfAiRobotFactoryEnabled =
        CVarDef.Create("malfai.robot_factory_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> MalfAiDoomsdayEnabled =
        CVarDef.Create("malfai.doomsday_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<float> MalfAiDoomsdayDuration =
        CVarDef.Create("malfai.doomsday_duration", 10f, CVar.SERVERONLY);

    public static readonly CVarDef<float> MalfAiDoomsdayWaveSpeed =
        CVarDef.Create("malfai.doomsday_wave_speed", 5f, CVar.SERVERONLY);

    public static readonly CVarDef<float> MalfAiDoomsdayWaveRadius =
        CVarDef.Create("malfai.doomsday_wave_radius", 250f, CVar.SERVERONLY);
}
