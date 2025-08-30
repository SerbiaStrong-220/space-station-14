using Content.Shared.Mind;

namespace Content.Shared.SS220.RoundEndInfo;

public interface ISharedRoundEndInfoManager
{
    public T EnsureInfo<T>() where T : class, IRoundEndInfo, new();
    public void ClearAllData();
    public IEnumerable<IRoundEndInfo> GetAllInfos();
}

public static class RoundEndInfoUtils
{
    public static string GetMindName(IEntityManager entMan, EntityUid? uid)
    {
        return entMan.TryGetComponent(uid, out MindComponent? mind)
            ? mind.CharacterName ?? Loc.GetString("game-ticker-unknown-role")
            : Loc.GetString("game-ticker-unknown-role");
    }

    public static (EntityUid?, int) GetTopBy<TData>(
        Dictionary<EntityUid, TData> dict,
        Func<TData, int> selector)
    {
        EntityUid? topUid = null;
        var topValue = 0;

        foreach (var (uid, data) in dict)
        {
            var value = selector(data);

            if (value <= topValue)
                continue;

            topValue = value;
            topUid = uid;
        }

        return (topUid, topValue);
    }
}
