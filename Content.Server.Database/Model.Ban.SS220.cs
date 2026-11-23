using System.ComponentModel.DataAnnotations.Schema;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Content.Server.Database;

public abstract class IBanRole
{
    public int Id { get; set; }

    [ForeignKey(nameof(Ban))]
    public int BanId { get; set; }

    public Ban? Ban { get; set; }
}

public sealed class BanSpecie : IBanRole
{
    public required string SpecieId { get; set; }
}

public sealed class BanChat : IBanRole
{
    public required string Chat { get; set; }
}
