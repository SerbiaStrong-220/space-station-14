using System.ComponentModel.DataAnnotations.Schema;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Content.Server.Database;

public abstract class IBanRole;

public sealed class BanSpecie : IBanRole
{
    public int Id { get; set; }

    public required string SpecieId { get; set; }

    /// <summary>
    /// The ID of the ban to which this applies.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }
}

public sealed class BanChat : IBanRole
{
    public int Id { get; set; }

    public required string Chat { get; set; }

    /// <summary>
    /// The ID of the ban to which this applies.
    /// </summary>
    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }
}
