namespace Content.Shared.Database;

public interface IBanRoleDef; // We'll use ToString() so it's empty

// if you change type - also change in BanChatsMsg
public enum BannableChats : byte
{
    Invalid,
    LOOC,
    OOC,
}

[Serializable]
public record struct BanChatDef(BannableChats Chat) : IBanRoleDef
{
    public override string ToString()
    {
        return Chat.ToString();
    }
}

[Serializable]
public record struct BanSpecieDef(string Specie) : IBanRoleDef
{
    public override string ToString()
    {
        return Specie;
    }
}
