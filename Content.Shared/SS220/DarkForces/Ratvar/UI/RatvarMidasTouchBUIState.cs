using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Ratvar.UI;

[Serializable, NetSerializable]
public sealed class RatvarMidasTouchBUIState : BoundUserInterfaceState
{
    public IReadOnlyCollection<string> Ids { get; set; }

    public RatvarMidasTouchBUIState(IReadOnlyCollection<string> ids)
    {
        Ids = ids;
    }
}

[Serializable, NetSerializable]
public sealed class RatvarTouchSelectedMessage : BoundUserInterfaceMessage
{
    public string Item { get; private set; }

    public RatvarTouchSelectedMessage(string item)
    {
        Item = item;
    }
}

[Serializable, NetSerializable]
public enum RatvarMidasTouchUIKey
{
    Key
}
