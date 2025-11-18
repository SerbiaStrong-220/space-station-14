// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.SS220.CultYogg.Rave;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.CultYogg.Rave;

public sealed class RaveSystem : SharedRaveSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Mumble(Entity<RaveComponent> ent)
    {
        base.Mumble(ent);

        if (_random.Prob(ent.Comp.SilentPhraseChance))
            _chat.TrySendInGameICMessage(ent, PickPhrase(ent.Comp.PhrasesPlaceholders), InGameICChatType.Whisper, ChatTransmitRange.Normal);
        else
            _chat.TrySendInGameICMessage(ent, PickPhrase(ent.Comp.PhrasesPlaceholders), InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    private string PickPhrase(string name)
    {
        var dataset = _proto.Index<DatasetPrototype>(name);
        return _random.Pick(dataset.Values);
    }
}
