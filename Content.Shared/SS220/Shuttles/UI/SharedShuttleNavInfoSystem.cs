
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Shuttles.UI;

public abstract class SharedShuttleNavInfoSystem : EntitySystem
{
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;

    public IReadOnlyDictionary<Type, ShuttleNavInfo> Infos => _infos;
    protected Dictionary<Type, ShuttleNavInfo> _infos = new();

    public bool AddInfo<T>(T info) where T : ShuttleNavInfo
    {
        var type = typeof(T);
        if (_infos.ContainsKey(type))
            return false;

        _infos.Add(type, info);
        UpdateClients();
        return true;
    }

    public bool RemoveInfo(Type type)
    {
        if (_infos.Remove(type))
        {
            UpdateClients();
            return true;
        }

        return false;
    }

    public bool RemoveInfo<T>() where T : ShuttleNavInfo
    {
        return RemoveInfo(typeof(T));
    }

    public void SetInfo<T>(T info) where T : ShuttleNavInfo
    {
        RemoveInfo<T>();
        AddInfo(info);
        UpdateClients();
    }

    public T? GetInfo<T>() where T : ShuttleNavInfo
    {
        if (_infos.TryGetValue(typeof(T), out var info))
            return (T)info;

        return default;
    }

    public bool TryGetInfo<T>([NotNullWhen(true)] out T? info) where T : ShuttleNavInfo
    {
        info = GetInfo<T>();
        return info != null;
    }

    public T EnsureInfo<T>() where T : ShuttleNavInfo, new()
    {
        if (TryGetInfo<T>(out var info))
            return info;
        else
        {
            info = _typeFactory.CreateInstance<T>();
            AddInfo(info);
            return info;
        }
    }

    public virtual void UpdateClients() { }
}

[Serializable, NetSerializable]
public sealed class ShuttleNavInfoUpdateMessage(HashSet<ShuttleNavInfo> infoLists) : EntityEventArgs
{
    public HashSet<ShuttleNavInfo> InfoLists = infoLists;
}
