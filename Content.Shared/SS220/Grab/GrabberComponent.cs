using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Grab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrabberComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 GrabOffset = new Vector2(0, -0.25f);

    [DataField, AutoNetworkedField]
    public EntityUid? Grabbing;

    [DataField, AutoNetworkedField]
    public TimeSpan GrabDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField, AutoNetworkedField]
    public Dictionary<GrabStage, float> GrabStagesSpeedModifier = new()
    {
        { GrabStage.Passive, 0.70f },
        { GrabStage.Aggressive, 0.50f },
        { GrabStage.NeckGrab, 0.40f },
        { GrabStage.Chokehold, 0.30f },
    };
}
