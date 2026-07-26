using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.BanList;

[Serializable, NetSerializable]
public sealed class BanListEuiState : EuiStateBase
{
    public BanListEuiState(string banListPlayerName, List<SharedBan> bans, List<SharedBan> roleBans, /* SS220-bans-begin */ List<SharedBan> speciesBans, List<SharedBan> chatBans /* SS220-ban-end */)
    {
        BanListPlayerName = banListPlayerName;
        Bans = bans;
        RoleBans = roleBans;
        SpeciesBans = speciesBans; // SS220 species bans
        ChatBans = chatBans; // SS220 chat bans
    }

    public string BanListPlayerName { get; }
    public List<SharedBan> Bans { get; }
    public List<SharedBan> RoleBans { get; }
    public List<SharedBan> SpeciesBans { get; } // SS220 species bans
    public List<SharedBan> ChatBans { get; } // SS220 chat bans
}
