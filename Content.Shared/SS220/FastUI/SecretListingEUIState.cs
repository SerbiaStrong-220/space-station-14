using Content.Shared.Eui;
using Content.Shared.SS220.FastUI;
using Robust.Shared.Serialization;

namespace Content.Shared.FastUI;


public static class SecretListingEUIState
{
    [Serializable, NetSerializable]
    public sealed class SecretListingEUIInitState : EuiMessageBase
    {
        public NetEntity NetEntity;
        public SecretListingCategoryPrototype Prototype;
        public SecretListingEUIInitState(SecretListingCategoryPrototype prototype, NetEntity netEntity)
        {
            Prototype = prototype;
            NetEntity = netEntity;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SelectItemEUIMessage : EuiMessageBase
    {
        public string Key;
        public ListingData Data;
        public NetEntity NetEntity;
        public SelectItemEUIMessage(string key, ListingData data, NetEntity netEntity)
        {
            Key = key;
            Data = data;
            NetEntity = netEntity;
        }
    }
}

