using Content.Shared.SS220.RoundEndInfo;

namespace Content.Client.SS220.RoundEndInfo;

public sealed class RoundEndInfoManager : ISharedRoundEndInfoManager
{
    public T EnsureInfo<T>() where T : class, IRoundEndInfo, new()
    {
        return null!;
    }

    public void ClearAllData()
    {
    }

    public IEnumerable<IRoundEndInfo> GetAllInfos()
    {
        return [];
    }
}
