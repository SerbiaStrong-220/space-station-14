// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Messenger;

/// <summary>
/// Id of a chat. It is unique within one messenger server, chats, messages and contacts share one counter.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct ChatUid(uint Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

/// <summary>
/// Id of a message. Ids grow with time, so a bigger id is a newer message of the server.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct MessageUid(uint Value) : IComparable<MessageUid>
{
    public int CompareTo(MessageUid other)
    {
        return Value.CompareTo(other.Value);
    }

    public static bool operator <(MessageUid left, MessageUid right)
    {
        return left.Value < right.Value;
    }

    public static bool operator >(MessageUid left, MessageUid right)
    {
        return left.Value > right.Value;
    }

    public static bool operator <=(MessageUid left, MessageUid right)
    {
        return left.Value <= right.Value;
    }

    public static bool operator >=(MessageUid left, MessageUid right)
    {
        return left.Value >= right.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

/// <summary>
/// Id of a contact of a messenger server. Contacts, chats and messages of one server share one counter,
/// the types do not let to mix them up.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct ContactUid(uint Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}
