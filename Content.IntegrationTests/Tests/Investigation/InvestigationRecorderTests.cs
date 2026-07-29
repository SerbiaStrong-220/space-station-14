#nullable enable
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Logs;
using Content.Server.Investigation;
using Content.Shared.Database;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Investigation;

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
            recorder.WritePosition(entity, null, new Vector2(10f, 20f), null, 0.15f);
            recorder.WritePosition(entity, null, new Vector2(15f, 25f), null, 0.15f);

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
            recorder.WritePosition(speaker, null, new Vector2(7f, -3f), null, 0.15f);

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
