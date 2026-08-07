// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Movement;
using Content.Shared.Implants.Components;
using Content.Shared.Radio;
using Content.Shared.SS220.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SS220;

/// <summary>
/// The station death rattle implanter should offer one verb per selectable channel, and picking one
/// should actually land on the implant it is holding.
/// </summary>
[TestOf(typeof(RattleChannelSelectorSystem))]
public sealed class RattleChannelSelectorTest : MovementTest
{
    private static readonly EntProtoId ImplanterProtoId = "DeathRattleImplanterStation";

    [Test]
    public async Task PickChannelBeforeInjecting()
    {
        var implanter = ToServer(await PlaceInHands(ImplanterProtoId));

        var implanterComp = SEntMan.GetComponent<ImplanterComponent>(implanter);
        var implant = implanterComp.ImplanterSlot.ContainerSlot?.ContainedEntity;
        Assert.That(implant, Is.Not.Null, $"{ImplanterProtoId} spawned without an implant inside.");

        var selector = SEntMan.GetComponent<RattleChannelSelectorComponent>(implant!.Value);
        var rattle = SEntMan.GetComponent<RattleOnTriggerComponent>(implant.Value);

        Assert.That(rattle.RadioChannel.Id, Is.EqualTo("Medical"), "The implant did not default to the medical channel.");
        Assert.That(selector.Channels, Does.Contain(rattle.RadioChannel),
            "The default channel is not selectable, so it could never be switched back to.");

        var verbSystem = Server.System<SharedVerbSystem>();

        var verbs = await GetChannelVerbs(verbSystem, implanter);
        Assert.That(verbs, Has.Count.EqualTo(selector.Channels.Count),
            "Expected exactly one verb per selectable channel.");
        Assert.That(verbs.Count(v => v.Disabled), Is.EqualTo(1),
            "Exactly the currently selected channel should be disabled.");

        // Pick a channel that is not the current one.
        var target = selector.Channels.First(c => c != rattle.RadioChannel);
        var targetName = await LocalizedChannelName(target);
        var verb = verbs.First(v => !v.Disabled && v.Text == targetName);

        await Server.WaitPost(() => verbSystem.ExecuteVerb(verb, SPlayer, implanter));

        Assert.That(rattle.RadioChannel, Is.EqualTo(target), "Selecting a channel did not change the implant.");

        // The newly selected channel is now the disabled one, and the old one became selectable again.
        verbs = await GetChannelVerbs(verbSystem, implanter);
        Assert.That(verbs.Single(v => v.Disabled).Text, Is.EqualTo(targetName));
    }

    /// <summary>
    /// A plain implanter with nothing in it must not sprout channel verbs.
    /// </summary>
    [Test]
    public async Task EmptyImplanterHasNoChannelVerbs()
    {
        var implanter = ToServer(await PlaceInHands("Implanter"));
        var verbSystem = Server.System<SharedVerbSystem>();

        Assert.That(await GetChannelVerbs(verbSystem, implanter), Is.Empty);
    }

    private async Task<List<Verb>> GetChannelVerbs(SharedVerbSystem verbSystem, EntityUid implanter)
    {
        var verbs = new List<Verb>();
        await Server.WaitPost(() =>
        {
            verbs = verbSystem.GetLocalVerbs(implanter, SPlayer, typeof(Verb))
                .Where(v => v.Category == VerbCategory.ChannelSelect)
                .ToList();
        });
        return verbs;
    }

    /// <summary>
    /// Loc resolves through IoC, so it has to be read on the server thread.
    /// </summary>
    private async Task<string> LocalizedChannelName(ProtoId<RadioChannelPrototype> channel)
    {
        var name = string.Empty;
        await Server.WaitPost(() => name = ProtoMan.Index(channel).LocalizedName);
        return name;
    }
}
