using Content.Shared.CharacterInfo;
using Robust.Client.GameObjects;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.CharacterInfo;

public sealed class AntagonistInfoSystem : EntitySystem
{
    public event Action<CharacterData>? OnAntagonistUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagonistInfoEvent>(OnAntagonistInfoEvent);
    }

    public void RequestAntagonistInfo(EntityUid? entity)
    {
        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestAntagonistInfoEvent(entity.Value));
    }

    private void OnAntagonistInfoEvent(AntagonistInfoEvent msg, EntitySessionEventArgs args)
    {
        var sprite = CompOrNull<SpriteComponent>(msg.AntagonistEntityUid);
        var data = new CharacterData(msg.JobTitle, msg.Objectives, string.Empty, sprite, Name(msg.AntagonistEntityUid));

        OnAntagonistUpdate?.Invoke(data);
    }
}
