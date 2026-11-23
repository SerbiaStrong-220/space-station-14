using Robust.Shared.Prototypes;
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
        public ProtoId<SecretListingCategoryPrototype> Category;
        public SecretListingEUIInitState(ProtoId<SecretListingCategoryPrototype> category, NetEntity netEntity)
        {
            Category = category;
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

