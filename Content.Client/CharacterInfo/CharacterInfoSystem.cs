using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.CharacterInfo;

public sealed class CharacterInfoSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _players = default!;

    public event Action<CharacterData>? OnCharacterUpdate;
    public event Action<CharacterData>? OnAntagonistUpdate;
    public event Action? OnCharacterDetached;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachSysMessage>(OnPlayerAttached);

        SubscribeNetworkEvent<CharacterInfoEvent>(OnCharacterInfoEvent);
        SubscribeNetworkEvent<AntagonistInfoEvent>(OnAntagonistInfoEvent);
    }

    public void RequestCharacterInfo()
    {
        var entity = _players.LocalPlayer?.ControlledEntity;

        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestCharacterInfoEvent(entity.Value));
    }

    public void RequestAntagonistInfo(EntityUid? entity)
    {
        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestAntagonistInfoEvent(entity.Value));
    }

    private void OnPlayerAttached(PlayerAttachSysMessage msg)
    {
        if (msg.AttachedEntity == default)
        {
            OnCharacterDetached?.Invoke();
        }
    }

    private void OnCharacterInfoEvent(CharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        var sprite = CompOrNull<SpriteComponent>(msg.EntityUid);
        var data = new CharacterData(msg.JobTitle, msg.Objectives, msg.Briefing, sprite, Name(msg.EntityUid));

        OnCharacterUpdate?.Invoke(data);
    }

    private void OnAntagonistInfoEvent(AntagonistInfoEvent msg, EntitySessionEventArgs args)
    {
        var sprite = CompOrNull<SpriteComponent>(msg.AntagonistEntityUid);
        var data = new CharacterData(msg.JobTitle, msg.Objectives, msg.Briefing, sprite, Name(msg.AntagonistEntityUid));

        OnAntagonistUpdate?.Invoke(data);
    }

    public readonly record struct CharacterData(
        string Job,
        Dictionary<string, List<ConditionInfo>> Objectives,
        string Briefing,
        SpriteComponent? Sprite,
        string EntityName
    );
}
