using Content.Shared.Database;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers;

/// <summary>
/// Stores info to create species ban records.
/// </summary>
/// <seealso cref="IBanManager.CreateRoleBan(CreateRoleBanInfo)"/>
[Access(typeof(BanManager), Other = AccessPermissions.Execute)]
public sealed class CreateSpeciesBanInfo : CreateBanInfo
{
    internal readonly HashSet<ProtoId<SpeciesPrototype>> SpeciesPrototypes = [];

    /// <param name="reason">The reason for the role ban.</param>
    public CreateSpeciesBanInfo(string reason) : base(reason)
    {
    }

    public CreateSpeciesBanInfo AddSpecie(ProtoId<SpeciesPrototype> protoId)
    {
        SpeciesPrototypes.Add(protoId);
        return this;
    }
}

/// <summary>
/// Stores info to create role ban records.
/// </summary>
/// <seealso cref="IBanManager.CreateRoleBan(CreateRoleBanInfo)"/>
[Access(typeof(BanManager), Other = AccessPermissions.Execute)]
public sealed class CreateChatsBanInfo : CreateBanInfo
{
    internal readonly HashSet<BannableChats> Chats = [];

    /// <param name="reason">The reason for the role ban.</param>
    public CreateChatsBanInfo(string reason) : base(reason)
    {
    }

    public CreateChatsBanInfo AddChat(BannableChats protoId)
    {
        Chats.Add(protoId);
        return this;
    }
}
