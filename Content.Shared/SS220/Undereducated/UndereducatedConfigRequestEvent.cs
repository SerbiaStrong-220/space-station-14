// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Undereducated;

[NetSerializable, Serializable]
public sealed class UndereducatedConfigRequestEvent : EntityEventArgs
{
    public NetEntity NetEntity;
    public string SelectedLanguage;
    public float Chance;
    public UndereducatedConfigRequestEvent(NetEntity ent, string selectedLanguage, float сhance)
    {
        NetEntity = ent;
        SelectedLanguage = selectedLanguage;
        Chance = сhance;
    }
}
