using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AdditionalInfoForRoundEnd;

public abstract partial class SharedAdditionalInfoForRoundEndSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed partial class RoundEndAdditionalInfoEvent : EntityEventArgs
{
    public List<RoundEndInfoDisplayBlock> AdditionalInfo = new();
}

[Serializable, NetSerializable]
public sealed class RoundEndInfoDisplayBlock
{
    public string Title = string.Empty;
    public string Body = string.Empty;
    public Color Color = new(30, 30, 30, 200);
}

[NetSerializable, Serializable]
public sealed class RoundEndAntagPurchaseData
{
    public string Name = string.Empty;
    public List<string> ItemPrototypes = new();
    public int TotalTC;
}

[NetSerializable, Serializable]
public sealed class RoundEndAntagItemsEvent : EntityEventArgs
{
    public List<RoundEndAntagPurchaseData> PlayerPurchases = new();
}

[Serializable]
[NetSerializable]
public sealed class FoodData
{
    public int AmountFood;
}

[Serializable]
[NetSerializable]
public sealed class GunData
{
    public int TotalShots;
}

[Serializable]
[NetSerializable]
public sealed class PuddleData
{
    public int TotalPuddle;
}

[Serializable]
[NetSerializable]
public sealed class DeathData
{
    public List<TimeSpan> TimeOfDeath = new();
}
