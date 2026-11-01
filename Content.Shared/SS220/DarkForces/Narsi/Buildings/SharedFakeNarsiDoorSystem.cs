using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings;

public abstract class SharedFakeNarsiDoorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedFakeNarsiDoorComponent, ComponentGetState>(OnFakeNarsiDoorGetState);
    }

    private void OnFakeNarsiDoorGetState(EntityUid uid, SharedFakeNarsiDoorComponent component, ref ComponentGetState args)
    {
        args.State = new SharedFakeNarsiDoorComponentState(component.FakeRsiPath, component.RealRsiPath);
    }
}

[Serializable, NetSerializable]
public sealed class SharedFakeNarsiDoorComponentState : ComponentState
{
    public string FakeRsiPath { get; init; }
    public string RealRsiPath { get; init; }
    public SharedFakeNarsiDoorComponentState(string fakeRsiPath, string realRsiPath)
    {
        FakeRsiPath = fakeRsiPath;
        RealRsiPath = realRsiPath;
    }
}
