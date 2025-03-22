// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Language;

[Serializable, NetSerializable]
public sealed class UpdateLanguageSeedEvent : EntityEventArgs
{
    public int Seed;

    public UpdateLanguageSeedEvent(int seed)
    {
        Seed = seed;
    }
}

[Serializable, NetSerializable]
public sealed class UpdateScrambledMessagesEvent : EntityEventArgs
{
    public Dictionary<string, LanguageMessage> ScrambledMessages = new();

    public UpdateScrambledMessagesEvent(Dictionary<string, LanguageMessage> scrambledMessages)
    {
        ScrambledMessages = scrambledMessages;
    }
}

[Serializable, NetSerializable]
public sealed class ClientRequireLanguageUpdateEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class ClientSelectLanguageEvent : EntityEventArgs
{
    public string LanguageId = string.Empty;

    public ClientSelectLanguageEvent(string languageId)
    {
        LanguageId = languageId;
    }
}

[Serializable, NetSerializable]
public sealed class ClientAddScrambledMessageEvent : EntityEventArgs
{
    public string Key;
    public LanguageMessage Message;

    public ClientAddScrambledMessageEvent(string key, LanguageMessage message)
    {
        Key = key;
        Message = message;
    }
}
