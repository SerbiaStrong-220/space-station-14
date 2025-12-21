using Content.Shared.Eui;
using Content.Shared.SS220.Administration.BanList;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.BanList;

[Serializable, NetSerializable]
public sealed class BanListEuiState : EuiStateBase
{
    public BanListEuiState(
        string banListPlayerName,
        List<SharedServerBan> bans,
        List<SharedServerRoleBan> roleBans,
        List<SharedServerSpeciesBan> speciesBans // SS220 Species bans
        )
    {
        BanListPlayerName = banListPlayerName;
        Bans = bans;
        RoleBans = roleBans;
        SpeciesBans = speciesBans; // SS220 Species bans
    }

    public string BanListPlayerName { get; }
    public List<SharedServerBan> Bans { get; }
    public List<SharedServerRoleBan> RoleBans { get; }
    public List<SharedServerSpeciesBan> SpeciesBans { get; } // SS220 Species bans
}
