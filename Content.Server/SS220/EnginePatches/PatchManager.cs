using System.Reflection;
using HarmonyLib;

namespace Content.Server.SS220.EnginePatches;

public sealed class Patcher
{
    public static readonly bool HARMONY_LOGGING = false;
    public static readonly bool HARMONY_DEBUG = false;


    public static bool PatchApplied { get; private set; } = false;

    public const string MAGIC = "HARMONYPATCH";

    public static void Patch(ILogManager logMan)
    {
        if (PatchApplied)
            return;

        Harmony.DEBUG = HARMONY_DEBUG;

        var sawmill = logMan.GetSawmill("Harmony");
        sawmill.Info("Applying Harmony patches...");

        var harmony = new Harmony("net.ss220.server.enginepatch");
        var assembly = Assembly.GetExecutingAssembly();
        harmony.PatchAll(assembly);

        sawmill.Info("Harmony patches applied");
        PatchApplied = true;
    }
}
