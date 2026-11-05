namespace Content.Shared.Database;

/// <summary>
/// Types of bans that can be stored in the database.
/// </summary>
public enum BanType : byte
{
    /// <summary>
    /// A ban from the entire server. If a player matches the ban info, they will be refused connection.
    /// </summary>
    Server,

    /// <summary>
    /// A ban from playing one or more roles.
    /// </summary>
    Role,

    // SS220-ban-types-begin
    /// <summary>
    /// A ban from using or ooc or looc chat
    /// </summary>
    Chat = 16,

    /// <summary>
    /// A ban from playing one or more species
    /// </summary>
    Species = 17,
    // SS220-ban-types-end
}

/// <summary>
/// A single role for a database role ban.
/// </summary>
/// <param name="RoleType">The type of role being banned, e.g. <c>Job</c>.</param>
/// <param name="RoleId">
/// The ID of the role being banned. This is likely a prototype ID based on <paramref name="RoleType"/>.
/// </param>
[Serializable]
public record struct BanRoleDef(string RoleType, string RoleId) : IBanRoleDef /* SSS220-role-bans-abstract */
{
    public override string ToString()
    {
        return $"{RoleType}:{RoleId}";
    }
}
