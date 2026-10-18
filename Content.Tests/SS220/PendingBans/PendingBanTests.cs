using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Database;
using Content.Shared.Database.SS220.PendingBans;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.SS220.PendingBans;

[TestFixture]
public sealed class PendingBanTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    private SqliteConnection _connection = null!;
    private DbContextOptions<SqliteServerDbContext> _options = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(_connection).Options;
        await using var db = new SqliteServerDbContext(_options);
        await db.Database.MigrateAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    [TestCase(BanType.Server)]
    [TestCase(BanType.Role)]
    [TestCase(BanType.Chat)]
    [TestCase(BanType.Species)]
    public async Task DelayPreservesDurationAndDoesNotActivateEarly(BanType type)
    {
        await using var db = new SqliteServerDbContext(_options);
        var ban = MakeBan(type, PendingBanTrigger.Delay);
        db.Ban.Add(ban);
        await db.SaveChangesAsync();

        Assert.That(await PendingBanActivation.ActivateAsync(db, "test", Now.AddMinutes(9)), Is.Empty);
        Assert.That(ban.Pending, Is.Not.Null);
        var applied = await PendingBanActivation.ActivateAsync(db, "test", Now.AddMinutes(10));

        Assert.Multiple(() =>
        {
            Assert.That(applied.Select(a => a.Id), Is.EqualTo(new[] { ban.Id }));
            Assert.That(ban.Pending, Is.Null);
            Assert.That(ban.BanTime, Is.EqualTo(Now.AddMinutes(10)));
            Assert.That(ban.ExpirationTime, Is.EqualTo(Now.AddMinutes(70)));
            Assert.That(applied[0].Erase, Is.True);
            Assert.That(applied[0].PostBanInfo, Is.True);
            Assert.That(ban.Reason, Is.EqualTo("pending test"));
        });
        Assert.That(await PendingBanActivation.ActivateAsync(db, "test", Now.AddDays(1), force: true), Is.Empty);
        Assert.That(await db.Ban.CountAsync(), Is.EqualTo(1));
    }

    [TestCase(PendingBanTrigger.RoundEnd)]
    [TestCase(PendingBanTrigger.RoundStart)]
    [TestCase(PendingBanTrigger.Lobby)]
    public async Task RoundConditionRequiresTheSelectedTransition(PendingBanTrigger trigger)
    {
        await using var db = new SqliteServerDbContext(_options);
        var ban = MakeBan(BanType.Role, trigger);
        db.Ban.Add(ban);
        await db.SaveChangesAsync();

        Assert.That(await PendingBanActivation.ActivateAsync(db, "test", Now.AddDays(1)), Is.Empty);
        var other = trigger == PendingBanTrigger.RoundEnd ? PendingBanTrigger.RoundStart : PendingBanTrigger.RoundEnd;
        Assert.That(await PendingBanActivation.ActivateAsync(db, "test", Now, other), Is.Empty);
        Assert.That(await PendingBanActivation.ActivateAsync(db, "test", Now, trigger), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ShutdownAndRecoveryForceAllTypesButOnlyForTheirServer()
    {
        int[] ids;
        await using (var before = new SqliteServerDbContext(_options))
        {
            var bans = new[]
            {
                MakeBan(BanType.Server, PendingBanTrigger.Delay),
                MakeBan(BanType.Role, PendingBanTrigger.RoundEnd),
                MakeBan(BanType.Chat, PendingBanTrigger.RoundStart),
                MakeBan(BanType.Species, PendingBanTrigger.Lobby),
            };
            bans[0].ExpirationTime = null; // Permanent bans must remain permanent.
            var otherServer = MakeBan(BanType.Server, PendingBanTrigger.Delay);
            otherServer.PendingServer = "other";
            before.Ban.AddRange(bans);
            before.Ban.Add(otherServer);
            await before.SaveChangesAsync();
            ids = bans.Select(b => b.Id).ToArray();
        }

        // A new context represents a restarted process: no in-memory queue is needed.
        await using var after = new SqliteServerDbContext(_options);
        var activated = await PendingBanActivation.ActivateAsync(after, "test", Now, force: true);
        Assert.That(activated.Select(b => b.Id), Is.EquivalentTo(ids));
        var stored = await after.Ban.OrderBy(b => b.Id).ToArrayAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Take(4).All(b => b.Pending == null), Is.True);
            Assert.That(stored[0].ExpirationTime, Is.Null);
            Assert.That(stored[4].PendingServer, Is.EqualTo("other"));
        });
        Assert.That(await PendingBanActivation.ActivateAsync(after, "test", Now, force: true), Is.Empty);
    }

    [Test]
    public async Task CancelledPendingBanNeverActivates()
    {
        await using var db = new SqliteServerDbContext(_options);
        var ban = MakeBan(BanType.Chat, PendingBanTrigger.Delay);
        ban.Unban = new Unban { UnbanTime = Now };
        db.Ban.Add(ban);
        await db.SaveChangesAsync();
        Assert.That(await PendingBanActivation.ActivateAsync(db, "test", Now.AddDays(1), force: true), Is.Empty);
    }

    [Test]
    public void MigrationSnapshotsMatchBothProviders()
    {
        using var sqlite = new SqliteServerDbContext(_options);
        using var postgres = new PostgresServerDbContext(
            new DbContextOptionsBuilder<PostgresServerDbContext>()
                .UseNpgsql("Host=localhost;Database=pending_bans_model_test")
                .Options);
        Assert.Multiple(() =>
        {
            Assert.That(sqlite.Database.HasPendingModelChanges(), Is.False);
            Assert.That(postgres.Database.HasPendingModelChanges(), Is.False);
        });
    }

    [TestCase(BanType.Server)]
    [TestCase(BanType.Role)]
    [TestCase(BanType.Chat)]
    [TestCase(BanType.Species)]
    public async Task ActiveLookupsExcludePendingBansButHistoryPreservesThem(BanType type)
    {
        var database = new ServerDbSqlite(
            () => _options, true, Mock.Of<IConfigurationManager>(), true,
            Mock.Of<ISawmill>(), Mock.Of<ISerializationManager>());
        var ban = MakeBan(type, PendingBanTrigger.Delay);
        ban.BanTime = DateTime.UtcNow;
        ban.ExpirationTime = ban.BanTime.AddHours(1);
        await using (var seed = new SqliteServerDbContext(_options))
        {
            seed.Ban.Add(ban);
            await seed.SaveChangesAsync();
        }

        var target = new NetUserId(ban.Players![0].UserId);
        Assert.That(await database.GetBanAsync(null, target, null, null, type), Is.Null);
        Assert.That(await database.GetBansAsync(null, target, null, null, false, type), Is.Empty);
        var history = await database.GetBansAsync(null, target, null, null, true, type);
        Assert.That(history, Has.Count.EqualTo(1));
        Assert.That(history[0].Pending, Is.EqualTo(ban.Pending));

        await database.ActivatePendingBansAsync("test", DateTime.UtcNow, null, true);
        var active = await database.GetBanAsync(null, target, null, null, type);
        Assert.That(active, Is.Not.Null);
        Assert.That(active!.Id, Is.EqualTo(ban.Id));
        Assert.That(active.Pending, Is.Null);
        Assert.That(await database.GetBansAsync(null, target, null, null, false, type), Has.Count.EqualTo(1));
    }

    private static Ban MakeBan(BanType type, PendingBanTrigger trigger)
    {
        return new Ban
        {
            Type = type,
            BanTime = Now,
            ExpirationTime = Now.AddHours(1),
            Reason = "pending test",
            Severity = NoteSeverity.Medium,
            Players = [new BanPlayer { UserId = Guid.NewGuid() }],
            Pending = new PendingBanData(
                "test", trigger == PendingBanTrigger.Delay ? Now.AddMinutes(10) : null, trigger, true, true),
            Roles = type switch
            {
                BanType.Role => [new BanRole { RoleType = "Job", RoleId = "Assistant" }],
                BanType.Chat => [new BanChat { Chat = "OOC" }],
                BanType.Species => [new BanSpecie { SpecieId = "Human" }],
                _ => [],
            },
        };
    }
}
