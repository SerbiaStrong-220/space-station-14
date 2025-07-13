using Content.Shared.SS220.Shuttles.UI;
using System.Linq;

namespace Content.Client.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShuttleNavInfoUpdateMessage>(OnUpdateMessage);
    }

    private void OnUpdateMessage(ShuttleNavInfoUpdateMessage msg)
    {
        var newDict = new Dictionary<Type, ShuttleNavInfo>();
        foreach (var value in msg.InfoLists)
        {
            var type = value.GetType();
            newDict.Add(type, value);
        }

        _infos = newDict;
    }
}
