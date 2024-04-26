using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Lidgren.Network;

namespace Content.Server.SS220.EnginePatches;

/// <summary>
/// This is a workaround for the issue when any client can easily DOS the server with IO operations of logger.
/// It simply removes logging calls by modifying IL code via Harmony transplier so there is no overhead.
///
/// IMPORTANT: Code must be re-verified every time Lidgren is updated.
/// - TheArturZh
/// </summary>
[HarmonyPatch(typeof(NetPeer))]
[HarmonyPatch("ReceiveSocketData")]
public static class NetPeer_ReceiveSocketData_CheckForErrors_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Cut out the warning message construction & log call.
        var warningStart = 0;
        var warningEnd = 0;
        var throwStart = 0;
        var throwEnd = 0;

        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldstr
                && codes[i].operand is string s)
            {
                if (s == Patcher.MAGIC)
                    return codes.AsEnumerable(); // Only apply patch once. If magic string is present, it's already patched.
                else if (s == "Malformed packet from ")
                    warningStart = i - 5; // -5 is offset to ldarg.0
                else if (s == "Unexpected NetMessageType: ")
                    throwStart = i - 1; // -1 is offset to ldarg.0
            }


            if (codes[i].opcode == OpCodes.Call
                && codes[i].operand is MethodInfo methodInfo)
            {
                if (methodInfo.Name == "LogWarning")
                    warningEnd = i;
                else if (methodInfo.Name == "ThrowOrLog")
                    throwEnd = i;
            }
        }

        if (Patcher.HARMONY_LOGGING)
        {
            FileLog.Log("Warning start: " + warningStart);
            FileLog.Log("Warning end: " + warningEnd);
            FileLog.Log("Throw start: " + throwStart);
            FileLog.Log("Throw end: " + throwEnd);
        }

        if (throwStart == 0 || throwEnd == 0 || warningStart == 0 || warningEnd == 0)
            throw new Exception("Failed to patch NetPeer.ReceiveSocketData. Check if engine has been updated. LENGTH:" + codes.Count + " INDEXES: warningStart: " + warningStart + ", warningEnd: " + warningEnd + ", throwStart: " + throwStart + ", throwEnd: " + throwEnd);

        for (var i = warningStart; i <= warningEnd; i++)
        {
            codes[i].opcode = OpCodes.Nop;
        }

        for (var i = throwStart; i <= throwEnd; i++)
        {
            codes[i].opcode = OpCodes.Nop;
        }

        // Put in the magic string to mark the assembly as patched.
        // This is done because Content can be loaded multiple times via same engine assembly.
        // Integration tests do it, for example.
        codes[warningEnd - 1].opcode = OpCodes.Ldstr;
        codes[warningEnd - 1].operand = Patcher.MAGIC;
        codes[warningEnd].opcode = OpCodes.Pop;

        return codes.AsEnumerable();
    }
}
