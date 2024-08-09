// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Shared.Random;
using Content.Server.Chat.Systems;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg;

public sealed class RaveSystem : SharedRaveSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    [ValidatePrototypeId<DatasetPrototype>]
    private const string PhrasesPlaceholders = "CultRlehPhrases";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaveComponent, ComponentStartup>(SetupRaving);
    }
    private void SetupRaving(Entity<RaveComponent> uid, ref ComponentStartup args)
    {
        uid.Comp.NextIncidentTime =
            _random.NextFloat(uid.Comp.TimeBetweenIncidents.X, uid.Comp.TimeBetweenIncidents.Y);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RaveComponent>();
        while (query.MoveNext(out var uid, out var raving))
        {
            raving.NextIncidentTime -= frameTime;

            if (raving.NextIncidentTime >= 0)
                continue;

            // Set the new time.
            raving.NextIncidentTime +=
                _random.NextFloat(raving.TimeBetweenIncidents.X, raving.TimeBetweenIncidents.Y);

            _chat.TrySendInGameICMessage(uid, PickPhrase(PhrasesPlaceholders), InGameICChatType.Speak, ChatTransmitRange.Normal);
        }
    }
    private string PickPhrase(string name)
    {
        var dataset = _proto.Index<DatasetPrototype>(name);
        return _random.Pick(dataset.Values);
    }
}
