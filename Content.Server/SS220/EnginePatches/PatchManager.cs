using System.Collections;
using System.Reflection;
using HarmonyLib;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Server.SS220.EnginePatches;

public sealed class Patcher
{
    public static readonly bool HARMONY_VERBOSE_LOGGING = true;
    public static readonly bool HARMONY_DEBUG = false;

    public static bool PatchApplied = false;

    public const string MAGIC = "HARMONYPATCH";

    private static void LogHarmonyMessages(ILogManager logMan)
    {
        var sawmill = logMan.GetSawmill("Harmony");
        foreach (var msg in FileLog.GetBuffer(true))
        {
            sawmill.Info(msg);
        }
    }

    /// <summary>
    /// Clear message queues of all NetPeers to prevent IO DOS.
    /// </summary>
    public static void ClearMessageQueues(ILogManager logMan)
    {
        var sawmill = logMan.GetSawmill("Patcher");
        sawmill.Info("Clearning message queues...");
        try
        {
            var netMan = IoCManager.Resolve<IServerNetManager>();

            if (netMan is not NetManager)
            {
                sawmill.Info("IServerNetManager is not NetManager, skipping message queue clearing");
                return;
            }

            var peerListField = netMan!.GetType().GetField("_netPeers", BindingFlags.Instance | BindingFlags.NonPublic);
            var peerList = peerListField!.GetValue(netMan) as IEnumerable<object>;
            sawmill.Info("Peer list: " + peerList!.GetType().FullName);

            foreach (var peerData in peerList!)
            {
                sawmill.Info("Peer data type: " + peerData.GetType().FullName);

                var peerField = peerData.GetType().GetField("Peer", BindingFlags.Instance | BindingFlags.Public);
                if (peerField == null)
                {
                    sawmill.Warning("Failed to find Peer field in peer data");
                    continue;
                }
                sawmill.Info("Peer field type: " + peerField.FieldType.FullName);

                var peer = peerField.GetValue(peerData) as NetPeer;
                if (peer == null)
                {
                    sawmill.Warning("NetPeer is null.");
                    continue;
                }
                sawmill.Info("Peer: " + peer!.GetType().FullName);

                var queueType = peer.GetType();
                if (queueType.Name == "NetServer")
                    queueType = queueType.BaseType;

                var queueField = queueType!.GetField("m_releasedIncomingMessages", BindingFlags.Instance | BindingFlags.NonPublic);
                if (queueField == null)
                {
                    sawmill.Warning("Failed to find m_releasedIncomingMessages field in " + peer.GetType().Name);
                    continue;
                }

                //NetQueue<NetIncomingMessage> m_releasedIncomingMessages;
                var queue = queueField.GetValue(peer) as NetQueue<NetIncomingMessage>;
                queue?.Clear();
            }
        }
        catch (Exception e)
        {
            sawmill.Error(e.ToString());
        }
        finally
        {
            sawmill.Info("Message queues successfully cleared.");
        }
    }

    public static void Patch(ILogManager logMan)
    {
        Harmony.DEBUG = HARMONY_DEBUG;

        var sawmill = logMan.GetSawmill("Harmony");
        if (PatchApplied)
        {
            sawmill.Info("Patches are already applied, skipping...");
            return;
        }
        sawmill.Info("Applying Harmony patches...");

        var harmony = new Harmony("net.ss220.server.enginepatch");
        var assembly = Assembly.GetExecutingAssembly();

        // Run magic-string-embedding patch first, to check whether this engine was already patched or not.
        // Necessary for the tests to run correctly.
        harmony.PatchCategory(assembly, "Magic");
        LogHarmonyMessages(logMan);
        if (PatchApplied)
        {
            sawmill.Info("Skipping patches...");
            return;
        }

        harmony.PatchAllUncategorized(assembly);
        LogHarmonyMessages(logMan);

        sawmill.Info("Harmony patches applied");
        PatchApplied = true;
    }
}
