using Content.Shared.SS220.RoundEndInfo;

namespace Content.Client.SS220.RoundEndInfo;

public sealed class RoundEndInfoManager : IRoundEndInfoManager
{
    /// <summary>
    /// Must always return null
    /// </summary>
    /// <returns>Null</returns>
    public T EnsureInfo<T>() where T : class, IRoundEndInfo, new()
    {
        return null!;
    }

    /// <summary>
    /// Must always return nothing
    /// </summary>
    /// <returns>Nothing</returns>
    public bool TryGetInfo<T>(out T info) where T : IRoundEndInfo
    {
        info = default!;
        return false;
    }

    /// <summary>
    /// Do nothing on a client
    /// </summary>
    public void ClearAllData()
    {
    }

    /// <summary>
    /// Return empty
    /// </summary>
    /// <returns>Empty info</returns>
    public IEnumerable<IRoundEndInfo> GetAllInfos()
    {
        return [];
    }
}
