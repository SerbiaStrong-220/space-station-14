// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using ClientTtsNetManager = Content.Client.SS220.TTS.TtsNetManager;

namespace Content.IntegrationTests.Tests.SS220.TTS;

[TestFixture]
[TestOf(typeof(MsgPlayTts))]
public sealed class MsgPlayTtsTest : GameTest
{
    /// <summary>
    /// A tiny valid Ogg Vorbis file (50 ms of silence)
    /// </summary>
    private static readonly byte[] AudioBytes = Convert.FromBase64String(
        "T2dnUwACAAAAAAAAAAAnKuHRAAAAAF1yeyYBHgF2b3JiaXMAAAAAAUAfAAAAAAAAgFcAAAAAAACZAU9nZ1MAAAAAAAAAAAAAJyrh" +
        "0QEAAAA0msrlCz7///////////+1A3ZvcmJpcwwAAABMYXZmNjMuMS4xMDIBAAAAHgAAAGVuY29kZXI9TGF2YzYzLjEuMTAyIGxp" +
        "YnZvcmJpcwEFdm9yYmlzEkJDVgEAAAEADFIUISUZU0pjCJVSUikFHWNQW0cdY9Q5RiFkEFOISRmle08qlVhKyBFSWClFHVNMU0mV" +
        "UpYpRR1jFFNIIVPWMWWhcxRLhkkJJWxNrnQWS+iZY5YxRh1jzlpKnWPWMUUdY1JSSaFzGDpmJWQUOkbF6GJ8MDqVokIovsfeUukt" +
        "hYpbir3XGlPrLYQYS2nBCGFz7bXV3EpqxRhjjDHGxeJTKILQkFUAAAEAAEAEAUJDVgEACgAAwlAMRVGA0JBVAEAGAIAAFEVxFMdx" +
        "HEeSJMsCQkNWAQBAAAACAAAojuEokiNJkmRZlmVZlqZ5lqi5qi/7ri7rru3qug6EhqwEAMgAABiGIYfeScyQU5BJJilVzDkIofUO" +
        "OeUUZNJSxphijFHOkFMMMQUxhtAphRDUTjmlDCIIQ0idZM4gSz3o4GLnOBAasiIAiAIAAIxBjCHGkHMMSgYhco5JyCBEzjkpnZRM" +
        "SiittJZJCS2V1iLnnJROSialtBZSy6SU1kIrBQAABDgAAARYCIWGrAgAogAAEIOQUkgpxJRiTjGHlFKOKceQUsw5xZhyjDHoIFTM" +
        "McgchEgpxRhzTjnmIGQMKuYchAwyAQAAAQ4AAAEWQqEhKwKAOAEAgyRpmqVpomhpmih6pqiqoiiqquV5pumZpqp6oqmqpqq6rqmq" +
        "rmx5nml6pqiqnimqqqmqrmuqquuKqmrLpqvatumqtuzKsm67sqzbnqrKtqm6sm6qrm27smzrrizbuuR5quqZput6pum6quvasuq6" +
        "su2ZpuuKqivbpuvKsuvKtq3Ksq5rpum6oqvarqm6su3Krm27sqz7puvqturKuq7Ksu7btq77sq0Lu+i6tq7Krq6rsqzrsi3rtmzb" +
        "QsnzVNUzTdf1TNN1Vde1bdV1bVszTdc1XVeWRdV1ZdWVdV11ZVv3TNN1TVeVZdNVZVmVZd12ZVeXRde1bVWWfV11ZV+Xbd33ZVnX" +
        "fdN1dVuVZdtXZVn3ZV33hVm3fd1TVVs3XVfXTdfVfVvXfWG2bd8XXVfXVdnWhVWWdd/WfWWYdZ0wuq6uq7bs66os676u68Yw67ow" +
        "rLpt/K6tC8Or68ax676u3L6Patu+8Oq2Mby6bhy7sBu/7fvGsamqbZuuq+umK+u6bOu+b+u6cYyuq+uqLPu66sq+b+u68Ou+Lwyj" +
        "6+q6Ksu6sNqyr8u6Lgy7rhvDatvC7tq6cMyyLgy37yvHrwtD1baF4dV1o6vbxm8Lw9I3dr4AAIABBwCAABPKQKEhKwKAOAEABiEI" +
        "FWMQKsYghBBSCiGkVDEGIWMOSsYclBBKSSGU0irGIGSOScgckxBKaKmU0EoopaVQSkuhlNZSai2m1FoMobQUSmmtlNJaaim21FJs" +
        "FWMQMuekZI5JKKW0VkppKXNMSsagpA5CKqWk0kpJrWXOScmgo9I5SKmk0lJJqbVQSmuhlNZKSrGl0kptrcUaSmktpNJaSam11FJt" +
        "rbVaI8YgZIxByZyTUkpJqZTSWuaclA46KpmDkkopqZWSUqyYk9JBKCWDjEpJpbWSSiuhlNZKSrGFUlprrdWYUks1lJJaSanFUEpr" +
        "rbUaUys1hVBSC6W0FkpprbVWa2ottlBCa6GkFksqMbUWY22txRhKaa2kElspqcUWW42ttVhTSzWWkmJsrdXYSi051lprSi3W0lKM" +
        "rbWYW0y5xVhrDSW0FkpprZTSWkqtxdZaraGU1koqsZWSWmyt1dhajDWU0mIpKbWQSmyttVhbbDWmlmJssdVYUosxxlhzS7XVlFqL" +
        "rbVYSys1xhhrbjXlUgAAwIADAECACWWg0JCVAEAUAABgDGOMQWgUcsw5KY1SzjknJXMOQggpZc5BCCGlzjkIpbTUOQehlJRCKSml" +
        "FFsoJaXWWiwAAKDAAQAgwAZNicUBCg1ZCQBEAQAgxijFGITGIKUYg9AYoxRjECqlGHMOQqUUY85ByBhzzkEpGWPOQSclhBBCKaWE" +
        "EEIopZQCAAAKHAAAAmzQlFgcoNCQFQFAFAAAYAxiDDGGIHRSOikRhExKJ6WREloLKWWWSoolxsxaia3E2EgJrYXWMmslxtJiRq3E" +
        "WGIqAADswAEA7MBCKDRkJQCQBwBAGKMUY845ZxBizDkIITQIMeYchBAqxpxzDkIIFWPOOQchhM455yCEEELnnHMQQgihgxBCCKWU" +
        "0kEIIYRSSukghBBCKaV0EEIIoZRSCgAAKnAAAAiwUWRzgpGgQkNWAgB5AACAMUo5JyWlRinGIKQUW6MUYxBSaq1iDEJKrcVYMQYh" +
        "pdZi7CCk1FqMtXYQUmotxlpDSq3FWGvOIaXWYqw119RajLXm3HtqLcZac865AADcBQcAsAMbRTYnGAkqNGQlAJAHAEAgpBRjjDmH" +
        "lGKMMeecQ0oxxphzzinGGHPOOecUY4w555xzjDHnnHPOOcaYc84555xzzjnnoIOQOeecc9BB6JxzzjkIIXTOOecchBAKAAAqcAAA" +
        "CLBRZHOCkaBCQ1YCAOEAAIAxlFJKKaWUUkqoo5RSSimllFICIaWUUkoppZRSSimllFJKKaWUUkoppZRSSimllFJKKaWUUkoppZRS" +
        "SimllFJKKaWUUkoppZRSSimllFJKKaWUUkoppZRSSimllFJKKaWUUkoppZRSSimllFJKKaWUUkoppZRSSimllFJKKZVSSimllFJK" +
        "KaWUUkoppQAg3woHAP8HG2dYSTorHA0uNGQlABAOAAAYwxiEjDknJaWGMQildE5KSSU1jEEopXMSUkopg9BaaqWk0lJKGYSUYgsh" +
        "lZRaCqW0VmspqbWUUigpxRpLSqml1jLnJKSSWkuttpg5B6Wk1lpqrcUQQkqxtdZSa7F1UlJJrbXWWm0tpJRaay3G1mJsJaWWWmup" +
        "xdZaTKm1FltLLcbWYkutxdhiizHGGgsA4G5wAIBIsHGGlaSzwtHgQkNWAgAhAQAEMko555yDEEIIIVKKMeeggxBCCCFESjHmnIMQ" +
        "QgghhIwx5yCEEEIIoZSQMeYchBBCCCGEUjrnIIRQSgmllFJK5xyEEEIIpZRSSgkhhBBCKKWUUkopIYQQSimllFJKKSWEEEIopZRS" +
        "SimlhBBCKKWUUkoppZQQQiillFJKKaWUEkIIoZRSSimllFJCCKWUUkoppZRSSighhFJKKaWUUkoJJZRSSimllFJKKSGUUkoppZRS" +
        "SimlAACAAwcAgAAj6CSjyiJsNOHCAxAAAAACAAJMAIEBgoJRCAKEEQgAAAAAAAgA+AAASAqAiIho5gwOEBIUFhgaHB4gIiQAAAAA" +
        "AAAAAAAAAAAET2dnUwAEkAEAAAAAAAAnKuHRAgAAAOp2qqgDAQEBAAAA");

    [Test]
    public async Task AudioWithFullMetadata()
    {
        var message = new MsgPlayTts(Audio(new TtsMetadata
        {
            Kind = TtsKind.Radio,
            Provider = TtsProvider.Silero,
            ChannelPrototype = "Common145.0",
            Source = new NetEntity(12),
            PlayEntity = new NetEntity(34),
        }));

        var received = await SendAndReceive(message);

        AssertAudio(received.Datas.Single(), message.Datas.Single());
    }

    [Test]
    public async Task AudioWithEmptyMetadata()
    {
        var message = new MsgPlayTts(Audio(new TtsMetadata { Kind = TtsKind.VoiceTest }));

        var received = await SendAndReceive(message);

        var data = received.Datas.Single();
        AssertAudio(data, message.Datas.Single());
        Assert.That(data.TtsMetadata.Provider, Is.Null);
        Assert.That(data.TtsMetadata.ChannelPrototype, Is.Null);
        Assert.That(data.TtsMetadata.Source, Is.Null);
        Assert.That(data.TtsMetadata.PlayEntity, Is.Null);
    }

    [Test]
    public async Task OnlyAudioLengthIsSent()
    {
        // A pooled buffer is bigger than the audio and must not be sent as is
        var padded = new byte[AudioBytes.Length + 500];
        Array.Fill(padded, (byte) 0xAB);
        AudioBytes.CopyTo(padded, 0);

        var message = new MsgPlayTts(new PlayTtsMessageData
        {
            TtsData = new TtsAudioBufferData(padded, AudioBytes.Length),
            TtsMetadata = new TtsMetadata { Kind = TtsKind.Say, Source = new NetEntity(5) },
        });

        var received = await SendAndReceive(message);

        var audio = (TtsAudioBufferData) received.Datas.Single().TtsData;
        Assert.That(audio.Length, Is.EqualTo(AudioBytes.Length));
        Assert.That(audio.Buffer, Is.EqualTo(AudioBytes), "Only the first Length bytes of the buffer should be sent.");
    }

    /// <summary>
    /// An announcement without TTS: a sound with params and no voice.
    /// </summary>
    [Test]
    public async Task SoundSpecifierWithParamsAndNoVoice()
    {
        var message = new MsgPlayTts(Sound(
            new SoundPathSpecifier("/Audio/SS220/Announcements/lifesigns.ogg", AudioParams.Default.WithVolume(-4)),
            new TtsMetadata { Kind = TtsKind.Announce }));

        var received = await SendAndReceive(message);

        var sound = (TtsSoundSpecifierData) received.Datas.Single().TtsData;
        var path = (SoundPathSpecifier) sound.SoundSpecifier;
        Assert.That(path.Path.ToString(), Is.EqualTo("/Audio/SS220/Announcements/lifesigns.ogg"));
        Assert.That(path.Params.Volume, Is.EqualTo(-4f));
    }

    /// <summary>
    /// An announcement with a sound and TTS: a sound specifier goes before the audio.
    /// </summary>
    [Test]
    public async Task SoundSpecifierThenAudio()
    {
        var metadata = new TtsMetadata { Kind = TtsKind.Announce, Provider = TtsProvider.NTTS };
        var message = new MsgPlayTts(
            Sound(new SoundCollectionSpecifier("PowerOn"), metadata),
            Audio(metadata));

        var received = await SendAndReceive(message);

        Assert.That(received.Datas, Has.Count.EqualTo(2));

        var sound = (TtsSoundSpecifierData) received.Datas[0].TtsData;
        Assert.That(((SoundCollectionSpecifier) sound.SoundSpecifier).Collection, Is.EqualTo("PowerOn"));

        AssertAudio(received.Datas[1], message.Datas[1]);
    }

    [Test]
    public async Task EmptyMessage()
    {
        var received = await SendAndReceive(new MsgPlayTts());

        Assert.That(received.Datas, Is.Empty);
    }

    private async Task<MsgPlayTts> SendAndReceive(MsgPlayTts message)
    {
        var serverNet = Server.ResolveDependency<IServerNetManager>();
        var clientTtsNet = Client.ResolveDependency<ClientTtsNetManager>();

        var session = ServerSession;
        Assert.That(session, Is.Not.Null, "The test pair has no player.");

        var received = new List<MsgPlayTts>();
        void OnReceived(MsgPlayTts msg) => received.Add(msg);

        await Client.WaitPost(() => clientTtsNet.PlayTtsReceived += OnReceived);
        try
        {
            await Server.WaitPost(() => serverNet.ServerSendMessage(message, session.Channel));
            await Pair.RunTicksSync(10);
        }
        finally
        {
            await Client.WaitPost(() => clientTtsNet.PlayTtsReceived -= OnReceived);
        }

        Assert.That(Pair.Client.Session, Is.Not.Null, "The client was disconnected while receiving the message.");
        Assert.That(received, Has.Count.EqualTo(1), "The client did not receive the message.");

        return received[0];
    }

    private static PlayTtsMessageData Audio(TtsMetadata metadata)
    {
        return new PlayTtsMessageData
        {
            TtsData = new TtsAudioBufferData(AudioBytes, AudioBytes.Length),
            TtsMetadata = metadata,
        };
    }

    private static PlayTtsMessageData Sound(SoundSpecifier specifier, TtsMetadata metadata)
    {
        return new PlayTtsMessageData
        {
            TtsData = new TtsSoundSpecifierData(specifier),
            TtsMetadata = metadata,
        };
    }

    private static void AssertAudio(PlayTtsMessageData actual, PlayTtsMessageData expected)
    {
        Assert.That(actual.TtsData, Is.TypeOf<TtsAudioBufferData>());

        var audio = (TtsAudioBufferData) actual.TtsData;
        Assert.That(audio.Length, Is.EqualTo(AudioBytes.Length));
        Assert.That(audio.Buffer, Is.EqualTo(AudioBytes));

        Assert.That(actual.TtsMetadata.Kind, Is.EqualTo(expected.TtsMetadata.Kind));
        Assert.That(actual.TtsMetadata.Provider, Is.EqualTo(expected.TtsMetadata.Provider));
        Assert.That(actual.TtsMetadata.ChannelPrototype, Is.EqualTo(expected.TtsMetadata.ChannelPrototype));
        Assert.That(actual.TtsMetadata.Source, Is.EqualTo(expected.TtsMetadata.Source));
        Assert.That(actual.TtsMetadata.PlayEntity, Is.EqualTo(expected.TtsMetadata.PlayEntity));
    }
}
