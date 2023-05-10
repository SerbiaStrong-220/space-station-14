using System.Security.Cryptography;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.SS220.Discord;

public sealed class DiscordPlayerManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private DiscordDbImpl _dbImpl = default!;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("DiscordPlayerManager");
        var host = _cfg.GetCVar(CCVars.PrimelistDatabaseIp);
        var port = _cfg.GetCVar(CCVars.PrimelistDatabasePort);
        var db = _cfg.GetCVar(CCVars.PrimelistDatabaseName);
        var user = _cfg.GetCVar(CCVars.PrimelistDatabaseUsername);
        var pass = _cfg.GetCVar(CCVars.PrimelistDatabasePassword);

        var builder = new DbContextOptionsBuilder<DiscordDbImpl.DiscordDbContext>();
        //TODO: поменять когда переедем на Posgresql
        var connectionString = new MySqlConnectionStringBuilder()
        {
            Server = host,
            Port = Convert.ToUInt32(port),
            Database = db,
            UserID = user,
            Password = pass,
        }.ConnectionString;

        _sawmill.Debug($"Using MySQL \"{host}:{port}/{db}\"");
        builder.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11 , 2)));
        _dbImpl = new DiscordDbImpl(builder.Options);
    }

    /// <summary>
    /// Проверка, генерация ключа для дискорда.
    /// Если валидация пройдена, то вернется пустая строка
    /// Если валидации не было, то вернется сгенерированный ключ
    /// </summary>
    /// <param name="playerData"></param>
    /// <returns></returns>
    public async Task<string> CheckAndGenerateKey(IPlayerData playerData)
    {
        var (validate, discordPlayer) = await _dbImpl.IsValidateDiscord(playerData.UserId);
        if (!validate)
        {
            if (discordPlayer != null)
                return discordPlayer.HashKey;

            discordPlayer = new DiscordDbImpl.DiscordPlayer()
            {
                CKey = playerData.UserName,
                SS14Id = playerData.UserId,
                HashKey = CreateSecureRandomString()
            };
            await _dbImpl.InsertDiscord(discordPlayer);
            return discordPlayer.HashKey;

        }

        return string.Empty;
    }

    private static string CreateSecureRandomString(int count = 32) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
}

internal sealed class DiscordDbImpl
{
    private readonly DbContextOptions<DiscordDbContext> _options;
    private readonly ISawmill _sawmill;

    public DiscordDbImpl(DbContextOptions<DiscordDbContext> options)
    {
        _options = options;
        _sawmill = Logger.GetSawmill("DiscordDbImpl");
    }

    /// <summary>
    /// Уже прошел проверку или нет
    /// </summary>
    /// <param name="ckey"></param>
    public async Task<(bool, DiscordPlayer?)> IsValidateDiscord(Guid playerId)
    {
        try
        {
            await using var db = await GetDb();

            var discordPlayer = await db.PgDbContext.DiscordPlayers.SingleOrDefaultAsync(p => p.SS14Id == playerId);

            if (discordPlayer == null)
                return (false, null);

            if (string.IsNullOrEmpty(discordPlayer.DiscordId))
            {
                return (true, null);
            }

            return (false, discordPlayer);
        }
        catch (Exception exception)
        {
            _sawmill.Log(LogLevel.Error, exception,"Error insert DiscordPlayer");
            throw;
        }
    }

    public async Task InsertDiscord(DiscordPlayer discordPlayer)
    {
        try
        {
            await using var db = await GetDb();
            db.PgDbContext.DiscordPlayers.Add(discordPlayer);
            await db.PgDbContext.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            _sawmill.Log(LogLevel.Error, exception,"Error insert DiscordPlayer");
            throw;
        }

    }


    private async Task<DbGuard> GetDb()
    {
        return new DbGuard(new DiscordDbContext(_options));
    }

    public sealed class DiscordDbContext : DbContext
    {
        public DiscordDbContext(DbContextOptions<DiscordDbContext> options) : base(options)
        {
        }

        public DbSet<DiscordPlayer> DiscordPlayers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscordPlayer>(entity =>
            {
                entity.HasIndex(p => p.Id).IsUnique();
                entity.HasAlternateKey(p => p.SS14Id);
                entity.Property(p => p.SS14Id).IsUnicode();
                entity.HasIndex(p => new { p.CKey, p.DiscordId });
                entity.Property(p => p.Id).ValueGeneratedOnAdd();
            });

        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.ConfigureWarnings(x =>
            {
                x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
    #if DEBUG
                    // for tests
                    x.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning);
    #endif
            });

    #if DEBUG
                options.EnableSensitiveDataLogging();
    #endif
        }
    }

    private sealed class DbGuard : IAsyncDisposable
    {
        public DbGuard(DiscordDbContext dbC)
        {
            PgDbContext = dbC;
        }

        public DiscordDbContext PgDbContext { get; }

        public ValueTask DisposeAsync()
        {
            return PgDbContext.DisposeAsync();
        }
    }

    public record DiscordPlayer
    {
        public Guid Id { get; set; }
        public Guid SS14Id { get; set; }
        public string HashKey { get; set; } = null!;
        public string CKey { get; set; } = null!;
        public string? DiscordId { get; set; }
        public string? DiscordName { get; set; }
    }
}
