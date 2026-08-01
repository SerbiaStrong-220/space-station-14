// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Logs;
using Content.Server.SS220.Investigation;
using Content.Shared.Database;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.SS220.Tests.Investigation;

[TestFixture]
[TestOf(typeof(InvestigationRecorder))]
public sealed class InvestigationRecorderTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        // The recorder is driven by round start/end, so we drive it by hand instead of racing the real ticker.
        DummyTicker = true,
        AdminLogsEnabled = true,
        Connected = false,
    };

    [Test]
    public async Task RecordsPositionsAndEnrichesAdminLogs()
    {
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var resources = server.ResolveDependency<IResourceManager>();
        var recorder = server.ResolveDependency<InvestigationRecorder>();
        var adminLogs = server.ResolveDependency<IAdminLogManager>();

        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        EntityUid entity = default;

        await server.WaitPost(() =>
        {
            recorder.StartRound(4242, "TestStation");

            entity = entities.SpawnEntity(null, coordinates);
            recorder.TrackEntity(entity, Guid.NewGuid(), "test_user", "Urist McTest", "MobHuman");

            // Two samples: the second is far enough away that it must produce its own row.
            SamplePosition(entities, entity, new Vector2(10f, 20f));
            SamplePosition(entities, entity, new Vector2(15f, 25f));

            // Goes through the real AdminLogManager.Add path, which is where the recorder hook lives.
            adminLogs.Add(LogType.MeleeHit, LogImpact.High, $"{entity:actor} did a test thing");

            recorder.StopRound(TimeSpan.FromMinutes(3));
        });

        Assert.That(recorder.LastBundlePath, Is.Not.Null, "Recorder did not report a finished bundle.");
        var dir = recorder.LastBundlePath!.Value;

        // --- meta.json ---
        var meta = JsonDocument.Parse(ReadAllText(resources, dir / "meta.json")).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(meta.GetProperty("roundId").GetInt32(), Is.EqualTo(4242));
            Assert.That(meta.GetProperty("map").GetString(), Is.EqualTo("TestStation"));
            Assert.That(meta.GetProperty("schema").GetInt32(), Is.EqualTo(InvestigationRecorder.SchemaVersion));
        });

        var roster = meta.GetProperty("roster").EnumerateArray().ToList();
        Assert.That(roster, Has.Count.EqualTo(1), "Tracked entity missing from the roster.");
        Assert.That(roster[0].GetProperty("e").GetInt32(), Is.EqualTo((int) entity));
        Assert.That(roster[0].GetProperty("name").GetString(), Is.EqualTo("Urist McTest"));

        // --- positions.jsonl.gz ---
        var positions = ReadJsonl(resources, dir / "positions.jsonl.gz");
        Assert.That(positions, Has.Count.EqualTo(2), "Expected exactly one row per distinct position.");
        Assert.Multiple(() =>
        {
            Assert.That(positions[0].GetProperty("e").GetInt32(), Is.EqualTo((int) entity));
            Assert.That(positions[0].GetProperty("x").GetDouble(), Is.EqualTo(10d).Within(0.01));
            Assert.That(positions[1].GetProperty("y").GetDouble(), Is.EqualTo(25d).Within(0.01));
        });

        // --- events.jsonl.gz: the admin log must arrive pre-joined to the last known position ---
        var events = ReadJsonl(resources, dir / "events.jsonl.gz");
        var melee = events.FirstOrDefault(e =>
            e.TryGetProperty("type", out var type) && type.GetString() == nameof(LogType.MeleeHit));

        Assert.That(melee.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined), "MeleeHit event was not recorded.");

        var actors = melee.GetProperty("entities").EnumerateArray().ToList();
        Assert.That(actors, Is.Not.Empty, "Admin log was recorded without any resolved entities.");

        var actor = actors[0];
        Assert.Multiple(() =>
        {
            Assert.That(actor.GetProperty("role").GetString(), Is.EqualTo("actor"));
            Assert.That(actor.GetProperty("e").GetInt32(), Is.EqualTo((int) entity));

            // This is the whole point of the hook: the log carries coordinates without any reader-side join.
            Assert.That(actor.GetProperty("x").GetDouble(), Is.EqualTo(15d).Within(0.01));
            Assert.That(actor.GetProperty("y").GetDouble(), Is.EqualTo(25d).Within(0.01));
        });
    }

    /// <summary>
    ///     A body that changes hands has to record every controller, not just the first, or the bundle
    ///     attributes a whole round of one player's actions to another.
    /// </summary>
    [Test]
    public async Task RecordsEveryControllerOfABody()
    {
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var resources = server.ResolveDependency<IResourceManager>();
        var recorder = server.ResolveDependency<InvestigationRecorder>();

        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        var firstPlayer = Guid.NewGuid();
        var secondPlayer = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            recorder.StartRound(4246, "TestStation");

            var body = entities.SpawnEntity(null, coordinates);
            recorder.TrackEntity(body, firstPlayer, "first", "Urist McCloned", "MobHuman");

            // One player spawns into the body, leaves it, and a different account picks it up — cloning,
            // borging and admin possession all look like this.
            recorder.WriteControl(body, firstPlayer, "first", attached: true);
            recorder.WriteControl(body, firstPlayer, "first", attached: false);
            recorder.WriteControl(body, secondPlayer, "second", attached: true);

            recorder.StopRound(TimeSpan.FromMinutes(1));
        });

        var rows = ReadJsonl(resources, recorder.LastBundlePath!.Value / "control.jsonl.gz");

        Assert.That(rows, Has.Count.EqualTo(3), "Every control change must produce a row.");
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].GetProperty("action").GetString(), Is.EqualTo("attach"));
            Assert.That(rows[1].GetProperty("action").GetString(), Is.EqualTo("detach"));
            Assert.That(rows[2].GetProperty("action").GetString(), Is.EqualTo("attach"));

            // The whole point: the second controller is a different account, and the bundle says so.
            Assert.That(rows[2].GetProperty("player").GetString(), Is.EqualTo(secondPlayer.ToString()));
            Assert.That(rows[2].GetProperty("userName").GetString(), Is.EqualTo("second"));
        });

        // The roster still names whoever held it first — that is its job, and it must not have been rewritten.
        var meta = JsonDocument.Parse(ReadAllText(resources, recorder.LastBundlePath!.Value / "meta.json")).RootElement;
        var roster = meta.GetProperty("roster").EnumerateArray().ToList();
        Assert.That(roster, Has.Count.EqualTo(1));
        Assert.That(roster[0].GetProperty("player").GetString(), Is.EqualTo(firstPlayer.ToString()));
    }

    /// <summary>
    ///     Objectives are recorded as a timeline, not a final tally: a row every time progress moves, so the
    ///     tick an objective was completed is recoverable.
    /// </summary>
    [Test]
    public async Task RecordsObjectiveProgressAndTheTickItCompleted()
    {
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var resources = server.ResolveDependency<IResourceManager>();
        var recorder = server.ResolveDependency<InvestigationRecorder>();

        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        EntityUid antagonist = default;
        EntityUid objective = default;

        await server.WaitPost(() =>
        {
            recorder.SetGamemode("Traitor", "Traitor");
            recorder.StartRound(4247, "TestStation");

            antagonist = entities.SpawnEntity(null, coordinates);
            objective = entities.SpawnEntity(null, coordinates);
            recorder.TrackEntity(antagonist, Guid.NewGuid(), "traitor", "Urist McTraitor", "MobHuman");

            // Untouched, then halfway, then done. The middle value must survive as its own row.
            recorder.WriteObjectiveIfChanged(objective, antagonist, "KillPersonObjective", "Kill the HoS", null, 0f);
            recorder.WriteObjectiveIfChanged(objective, antagonist, "KillPersonObjective", "Kill the HoS", null, 0.5f);
            // Repeated identical progress must not produce a second row.
            recorder.WriteObjectiveIfChanged(objective, antagonist, "KillPersonObjective", "Kill the HoS", null, 0.5f);
            recorder.WriteObjectiveIfChanged(objective, antagonist, "KillPersonObjective", "Kill the HoS", null, 1f);

            recorder.StopRound(TimeSpan.FromMinutes(1));
        });

        var directory = recorder.LastBundlePath!.Value;
        var rows = ReadJsonl(resources, directory / "objectives.jsonl.gz");

        Assert.That(rows, Has.Count.EqualTo(3), "One row per real change, and no row for a repeat.");
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].GetProperty("progress").GetDouble(), Is.EqualTo(0d).Within(0.001));
            Assert.That(rows[0].GetProperty("done").GetBoolean(), Is.False);
            Assert.That(rows[1].GetProperty("progress").GetDouble(), Is.EqualTo(0.5d).Within(0.001));

            // The whole point of the stream: which row says it was finished, and therefore when.
            Assert.That(rows[2].GetProperty("done").GetBoolean(), Is.True);
            Assert.That(rows[2].GetProperty("o").GetInt32(), Is.EqualTo((int) objective));
            Assert.That(rows[2].GetProperty("e").GetInt32(), Is.EqualTo((int) antagonist));
            Assert.That(rows[2].GetProperty("proto").GetString(), Is.EqualTo("KillPersonObjective"));
            Assert.That(rows[2].GetProperty("title").GetString(), Is.EqualTo("Kill the HoS"));
        });

        // Gamemode has to reach meta.json, or the antag list cannot be interpreted.
        var meta = JsonDocument.Parse(ReadAllText(resources, directory / "meta.json")).RootElement;
        Assert.That(meta.GetProperty("gamemode").GetString(), Is.EqualTo("Traitor"));
    }

    [Test]
    public async Task RecordsChatWithBareTextAndInlinePosition()
    {
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var resources = server.ResolveDependency<IResourceManager>();
        var recorder = server.ResolveDependency<InvestigationRecorder>();

        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        EntityUid speaker = default;

        await server.WaitPost(() =>
        {
            recorder.StartRound(4243, "TestStation");

            speaker = entities.SpawnEntity(null, coordinates);
            recorder.TrackEntity(speaker, Guid.NewGuid(), "chatter", "Urist McSpeaker", "MobHuman");
            SamplePosition(entities, speaker, new Vector2(7f, -3f));

            recorder.OnChat(speaker, "Say", "привет, мир", "Urist McSpeaker");
            recorder.OnChat(speaker, "Whisper", "тихо", "Замаскированный");
            // No in-world speaker, so there must be no entity and no position.
            recorder.OnChat(null, "OOC", "ooc line", "some_player");
            // Blank messages must not produce rows at all.
            recorder.OnChat(speaker, "Say", "   ", "Urist McSpeaker");

            recorder.StopRound(TimeSpan.FromMinutes(1));
        });

        var dir = recorder.LastBundlePath!.Value;
        var rows = ReadJsonl(resources, dir / "chat.jsonl.gz");

        Assert.That(rows, Has.Count.EqualTo(3), "Blank chat should be dropped, everything else kept.");

        var say = rows[0];
        Assert.Multiple(() =>
        {
            // The whole point of this stream: the bare text, not a formatted log sentence.
            Assert.That(say.GetProperty("msg").GetString(), Is.EqualTo("привет, мир"));
            Assert.That(say.GetProperty("ch").GetString(), Is.EqualTo("Say"));
            Assert.That(say.GetProperty("e").GetInt32(), Is.EqualTo((int) speaker));
            // Carried inline so a reader can place a bubble with no join.
            Assert.That(say.GetProperty("x").GetDouble(), Is.EqualTo(7d).Within(0.01));
            Assert.That(say.GetProperty("y").GetDouble(), Is.EqualTo(-3d).Within(0.01));
        });

        // A voice mask means the displayed name differs from the character; both must survive.
        Assert.That(rows[1].GetProperty("name").GetString(), Is.EqualTo("Замаскированный"));

        var ooc = rows[2];
        Assert.Multiple(() =>
        {
            Assert.That(ooc.GetProperty("ch").GetString(), Is.EqualTo("OOC"));
            Assert.That(ooc.TryGetProperty("e", out _), Is.False, "OOC has no in-world speaker.");
            Assert.That(ooc.TryGetProperty("x", out _), Is.False, "OOC must not carry a position.");
        });
    }

    /// <summary>
    ///     A stationary stretch has to be closed by a row before the entity moves again, or a reader
    ///     interpolating between the rows either side of it draws a slow drift across the room where the
    ///     character was actually standing still.
    /// </summary>
    [Test]
    public async Task ClosesAStationaryStretchBeforeMovingAgain()
    {
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var resources = server.ResolveDependency<IResourceManager>();
        var recorder = server.ResolveDependency<InvestigationRecorder>();
        var cfg = server.ResolveDependency<IConfigurationManager>();

        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        EntityUid entity = default;

        // The automatic sampler walks every tracked entity, and rows it added between the ticks below would be
        // indistinguishable from the ones under test. Pushed out of reach so only the samples here produce rows.
        cfg.SetCVar(CCVars220.InvestigationPositionInterval, 3600f);
        await server.WaitPost(() =>
        {
            recorder.StartRound(4245, "TestStation");
            entity = entities.SpawnEntity(null, coordinates);
            SamplePosition(entities, entity, new Vector2(0f, 0f));
        });

        // Standing perfectly still. None of these may produce a row on their own.
        for (var stationarySample = 0; stationarySample < 3; stationarySample++)
        {
            await server.WaitRunTicks(5);
            await server.WaitPost(() => SamplePosition(entities, entity, new Vector2(0f, 0f)));
        }

        // Then walking away in a straight line, which the dead reckoning collapses to its endpoints.
        await server.WaitRunTicks(5);
        await server.WaitPost(() => SamplePosition(entities, entity, new Vector2(2f, 0f)));
        await server.WaitRunTicks(5);
        await server.WaitPost(() => SamplePosition(entities, entity, new Vector2(4f, 0f)));

        await server.WaitPost(() => recorder.StopRound(TimeSpan.FromMinutes(1)));

        var rows = ReadJsonl(resources, recorder.LastBundlePath!.Value / "positions.jsonl.gz");

        // Spawn, the anchor closing the stationary stretch, and the last sample flushed at round end.
        Assert.That(rows, Has.Count.EqualTo(3), "Expected exactly one row closing the stationary stretch.");

        Assert.Multiple(() =>
        {
            Assert.That(rows[1].GetProperty("x").GetDouble(), Is.EqualTo(0d).Within(0.01),
                "The anchor must repeat the position that was held, not the one moved to.");
            Assert.That(rows[1].GetProperty("t").GetUInt32(), Is.GreaterThan(rows[0].GetProperty("t").GetUInt32()),
                "The anchor must be dated at the end of the stationary stretch, not at its start.");
            Assert.That(rows[2].GetProperty("x").GetDouble(), Is.EqualTo(4d).Within(0.01));
        });

        cfg.SetCVar(CCVars220.InvestigationPositionInterval, 0.5f);
    }

    [Test]
    public async Task WritesMetaAndRosterBeforeTheRoundEnds()
    {
        var server = Pair.Server;
        var entities = server.ResolveDependency<IEntityManager>();
        var resources = server.ResolveDependency<IResourceManager>();
        var recorder = server.ResolveDependency<InvestigationRecorder>();

        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;

        ResPath dir = default;

        await server.WaitPost(() =>
        {
            recorder.StartRound(4244, "TestStation");
            dir = recorder.CurrentBundlePath!.Value;

            var entity = entities.SpawnEntity(null, coordinates);
            recorder.TrackEntity(entity, Guid.NewGuid(), "early", "Urist McEarly", "MobHuman");
            recorder.Flush();
        });

        // Everything below is asserted while the round is still recording, which is what a crashed
        // server would leave behind. Without this, meta.json only appeared at StopRound and a crash
        // lost the roster entirely.
        var meta = JsonDocument.Parse(ReadAllText(resources, dir / "meta.json")).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(meta.GetProperty("roundId").GetInt32(), Is.EqualTo(4244));
            Assert.That(meta.TryGetProperty("durationSeconds", out _), Is.False,
                "An unfinished round must not claim a duration.");
        });

        var roster = ReadJsonl(resources, dir / "roster.jsonl.gz");
        Assert.That(roster, Has.Count.EqualTo(1));
        Assert.That(roster[0].GetProperty("name").GetString(), Is.EqualTo("Urist McEarly"));

        await server.WaitPost(() => recorder.StopRound(TimeSpan.FromMinutes(2)));

        var finalMeta = JsonDocument.Parse(ReadAllText(resources, dir / "meta.json")).RootElement;
        Assert.That(finalMeta.GetProperty("durationSeconds").GetDouble(), Is.EqualTo(120d).Within(0.1));
    }

    [Test]
    public async Task DoesNotWriteWhenNotRecording()
    {
        var server = Pair.Server;
        var recorder = server.ResolveDependency<InvestigationRecorder>();
        var adminLogs = server.ResolveDependency<IAdminLogManager>();

        await server.WaitPost(() =>
        {
            Assert.That(recorder.IsRecording, Is.False);

            // Must be a no-op rather than throwing: admin logs fire constantly outside of rounds.
            adminLogs.Add(LogType.Unknown, LogImpact.Low, $"log outside of a round");
        });
    }

    /// <summary>Drives one position sample through the system, which owns the dead-reckoning filter.</summary>
    private static void SamplePosition(IEntityManager entities, EntityUid uid, Vector2 local)
    {
        var system = entities.System<InvestigationRecorderSystem>();
        var tracked = entities.EnsureComponent<InvestigationTrackedComponent>(uid);
        var tick = IoCManager.Resolve<IGameTiming>().CurTick.Value;

        system.RecordPosition((uid, tracked), tick, new SampledPosition(null, 0, local, null));
    }

    private static string ReadAllText(IResourceManager resources, ResPath path)
    {
        using var stream = resources.UserData.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static List<JsonElement> ReadJsonl(IResourceManager resources, ResPath path)
    {
        using var stream = resources.UserData.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        var rows = new List<JsonElement>();
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                rows.Add(JsonDocument.Parse(line).RootElement.Clone());
        }

        return rows;
    }
}
