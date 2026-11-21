using Robust.Shared.Serialization;

namespace Content.Shared.SS220.FastUI;

[Serializable, NetSerializable]
public enum SecretListingKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SecretListingInitDataState : BoundUserInterfaceState
{
    public string Key;
    public List<ListingData> Data;
    public string WindowName;
    public string WindowDescription;
    public NetEntity UserEntity;
    public SecretListingInitDataState(string key, List<ListingData> data, string windowName, string windowDescription, NetEntity userEntity)
    {
        Key = key;
        Data = data;
        WindowName = windowName;
        WindowDescription = windowDescription;
        UserEntity = userEntity;
    }
}

[Serializable, NetSerializable]
public sealed class SecretListingInitState : BoundUserInterfaceState
{
    public SecretListingCategoryPrototype Prototype;
    public NetEntity UserEntity;
    public SecretListingInitState(SecretListingCategoryPrototype prototype, NetEntity userEntity)
    {
        Prototype = prototype;
        UserEntity = userEntity;
    }
}

[Serializable, NetSerializable]
public sealed class SelectItemMessage : BoundUserInterfaceMessage
{
    public string Key;
    public ListingData Data;
    public NetEntity UserEntity;

    public SelectItemMessage(string key, ListingData data, NetEntity userEntity)
    {
        Key = key;
        Data = data;
        UserEntity = userEntity;
    }
}
