// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Language;

[Serializable, NetSerializable]
public sealed class UpdateLanguageSeedEvent(int seed) : EntityEventArgs
{
    public int Seed = seed;
}

[Serializable, NetSerializable]
public sealed class UpdateClientPaperLanguageNodeInfo(string key, string info) : EntityEventArgs
{
    public string Key = key;
    public string Info = info;
}

[Serializable, NetSerializable]
public sealed class ClientRequireLanguageUpdateEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class ClientSelectLanguageEvent(string languageId) : EntityEventArgs
{
    public string LanguageId = languageId;
}

[Serializable, NetSerializable]
public sealed class ClientRequestPaperLanguageNodeInfo(string key) : EntityEventArgs
{
    public string Key = key;
}
