// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Language;

[Serializable, NetSerializable]
public sealed class ClientSelectLanguageEvent : EntityEventArgs
{
    public string LanguageId = string.Empty;

    public ClientSelectLanguageEvent(string languageId)
    {
        LanguageId = languageId;
    }
}

