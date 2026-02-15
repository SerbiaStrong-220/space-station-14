using System.Numerics;
using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Grab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrabberComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 GrabOffset = new Vector2(0, -0.25f);

    [DataField, AutoNetworkedField]
    public EntityUid? Grabbing;

    [DataField, AutoNetworkedField]
    public float Range = 1f;

    /// <summary>
    /// Delay used when failed to get grab delay from GrabDelays dictionary
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FallbackGrabDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public Dictionary<GrabStage, TimeSpan> GrabDelays = new()
    {
        { GrabStage.Passive, TimeSpan.FromSeconds(0.5f) },
        { GrabStage.Aggressive, TimeSpan.FromSeconds(1f) },
        { GrabStage.NeckGrab, TimeSpan.FromSeconds(2) },
        { GrabStage.Chokehold, TimeSpan.FromSeconds(2) },
    };

    [DataField, AutoNetworkedField]
    public Dictionary<GrabStage, float> GrabStagesSpeedModifier = new()
    {
        { GrabStage.Passive, 0.70f },
        { GrabStage.Aggressive, 0.50f },
        { GrabStage.NeckGrab, 0.40f },
        { GrabStage.Chokehold, 0.30f },
    };

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "Grabbing";

    [DataField, AutoNetworkedField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField]
    public LocId NewGrabPopup = "grabber-component-new-grab-popup";

    [DataField]
    public LocId GrabUpgradePopup = "grabber-component-grab-upgrade-popup";
}
