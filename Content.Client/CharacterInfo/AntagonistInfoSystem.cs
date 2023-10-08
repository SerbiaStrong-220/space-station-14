using Content.Shared.CharacterInfo;
using Robust.Client.UserInterface;
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
        var entity = GetEntity(msg.AntagonistEntityUid);
        var data = new CharacterData(entity, msg.JobTitle, msg.Objectives, null, Name(entity));

        OnAntagonistUpdate?.Invoke(data);
    }

    public List<Control> GetCharacterInfoControls(EntityUid uid)
    {
        var ev = new GetCharacterInfoControlsEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);
        return ev.Controls;
    }
}
