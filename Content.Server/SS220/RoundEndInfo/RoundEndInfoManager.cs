using Content.Shared.SS220.RoundEndInfo;

namespace Content.Server.SS220.RoundEndInfo;

/// <summary>
/// Manages all IRoundEndInfo data used for compiling round-end summaries.
/// Responsible for creating, storing, retrieving, and clearing instances of various info providers.
/// </summary>
public sealed class RoundEndInfoManager : IRoundEndInfoManager
{
    private readonly Dictionary<Type, IRoundEndInfo> _infos = new();

    /// <summary>
    /// Ensures that an instance of the specified IRoundEndInfo type exists and returns it.
    /// If none exists, a new one is created, initialized if applicable, and stored.
    /// </summary>
    /// <typeparam name="T">The type implementing IRoundEndInfo.</typeparam>
    /// <returns>An instance of the requested IRoundEndInfo type.</returns>
    public T EnsureInfo<T>() where T : class, IRoundEndInfo, new()
    {
        if (_infos.TryGetValue(typeof(T), out var existing))
            return (T) existing;

        var instance = IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance<T>();
        _infos.Add(typeof(T), instance);
        return instance;
    }

    public bool TryGetInfo<T>(out T info) where T : IRoundEndInfo
    {
        if (!_infos.TryGetValue(typeof(T), out var existing))
        {
            info = default!;
            return false;
        }

        info = (T) existing;
        return true;
    }

    /// <summary>
    /// Clears all stored IRoundEndInfo instances from the manager.
    /// </summary>
    public void ClearAllData()
    {
        _infos.Clear();
    }

    /// <summary>
    /// Returns an enumeration of all currently stored IRoundEndInfo instances.
    /// </summary>
    public IEnumerable<IRoundEndInfo> GetAllInfos()
    {
        return _infos.Values;
    }
}
