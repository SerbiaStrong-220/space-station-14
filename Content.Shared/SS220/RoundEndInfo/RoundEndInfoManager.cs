using System.Linq;
using Content.Shared.Mind;

namespace Content.Shared.SS220.RoundEndInfo;

/// <summary>
/// Shared realization server manager
/// On a client all method's must return null or do nothing.
/// </summary>
public interface IRoundEndInfoManager
{
    public T EnsureInfo<T>() where T : class, IRoundEndInfo, new();
    public bool TryGetInfo<T>(out T info) where T : IRoundEndInfo;
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
        if (dict.Count == 0)
            return (null, 0);

        var topPair = dict.MaxBy(kvp => selector(kvp.Value));
        return (topPair.Key, selector(topPair.Value));
    }
}
