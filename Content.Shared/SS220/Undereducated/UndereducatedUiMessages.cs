// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Undereducated;

[Serializable, NetSerializable]
public sealed class UndereducatedConfigRequest : BoundUserInterfaceMessage
{
    public string SelectedLanguage;
    public float Chance;

    public UndereducatedConfigRequest(string selectedLanguage, float chance)
    {
        SelectedLanguage = selectedLanguage;
        Chance = chance;
    }
}

[NetSerializable, Serializable]
public enum UndereducatedUiKey : byte
{
    Key
}
