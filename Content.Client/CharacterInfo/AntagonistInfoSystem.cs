using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Content.Shared.SS220.Objectives;
using Robust.Client.UserInterface;

namespace Content.Client.CharacterInfo;

public sealed class AntagonistInfoSystem : EntitySystem
{
    public event Action<AntagonistData>? OnAntagonistUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagonistInfoEvent>(OnAntagonistInfoEvent);

        // ss220 add custom goals x2 start
        SubscribeNetworkEvent<UpdateAntagonistInfoEvent>(OnUpdateAntagonistInfo);
        // ss220 add custom goals x2 end
    }

    public void RequestAntagonistInfo(EntityUid? entity)
    {
        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestAntagonistInfoEvent(GetNetEntity(entity.Value)));
    }

    private void OnAntagonistInfoEvent(AntagonistInfoEvent msg, EntitySessionEventArgs args)
    {
        var entity = GetEntity(msg.AntagonistNetEntity);
        var data = new AntagonistData(entity, msg.JobTitle, msg.Objectives, Name(entity));

        OnAntagonistUpdate?.Invoke(data);
    }

    // ss220 add custom goals x2 start
    private void OnUpdateAntagonistInfo(UpdateAntagonistInfoEvent ev)
    {
        var target = GetEntity(ev.Target);
        RequestAntagonistInfo(target);
    }
    // ss220 add custom goals x2 end

    public List<Control> GetAntagonistInfoControls(EntityUid uid)
    {
        var ev = new GetAntagonistInfoControlsEvent(uid);
        RaiseLocalEvent(uid, ref ev);
        return ev.Controls;
    }

    public readonly record struct AntagonistData(
        EntityUid Entity,
        string Job,
        Dictionary<string, List<ObjectiveInfo>> Objectives,
        string EntityName
    );

    /// <summary>
    /// Event raised to get additional controls to display in the antagonist info menu.
    /// </summary>
    [ByRefEvent]
    public readonly record struct GetAntagonistInfoControlsEvent(EntityUid Entity)
    {
        public readonly List<Control> Controls = new();

        public readonly EntityUid Entity = Entity;
    }
}
