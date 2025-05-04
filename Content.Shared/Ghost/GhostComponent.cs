using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGhostSystem))]
[AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class GhostComponent : Component
{
    // Actions
    [DataField]
    public EntProtoId ToggleLightingAction = "ActionToggleLighting";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleLightingActionEntity;

    [DataField]
    public EntProtoId ToggleFoVAction = "ActionToggleFov";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleFoVActionEntity;

    [DataField]
    public EntProtoId ToggleGhostsAction = "ActionToggleGhosts";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleGhostsActionEntity;

    [DataField]
    public EntProtoId ToggleGhostHearingAction = "ActionToggleGhostHearing";

    [DataField]
    public EntityUid? ToggleGhostHearingActionEntity;

    // SS220 ADD GHOST HUD'S START
    [DataField]
    public EntProtoId ToggleHudOnOtherAction = "ActionToggleHudOnOther";

    [DataField]
    public EntityUid? ToggleHudOnOtherActionEntity;
    // SS220 ADD GHOST HUD'S END

    [DataField]
    public EntProtoId BooAction = "ActionGhostBoo";

    [DataField, AutoNetworkedField]
    public EntityUid? BooActionEntity;

    //SS-220 noDeath
    [DataField]
    public EntProtoId RespawnAction = "ActionRespawn";

    [DataField, AutoNetworkedField]
    public EntityUid? RespawnActionEntity;
    //SS-220 end noDeath

    //SS220-ghost-hats begin
    [DataField]
    public EntProtoId ToggleAGhostBodyVisualsAction = "ActionToggleAGhostBodyVisuals";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleAGhostBodyVisualsActionEntity;
    //SS220-ghost-hats end
    // End actions

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoPausedField]
    public TimeSpan TimeOfDeath = TimeSpan.Zero;

    [DataField("booRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float BooRadius = 3;

    [DataField("booMaxTargets"), ViewVariables(VVAccess.ReadWrite)]
    public int BooMaxTargets = 3;

    //SS220-ghost-hats begin
    /// <summary>
    /// Whether the ghost's body is visible.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool BodyVisible = true;
    //SS220-ghost-hats end

    // TODO: instead of this funny stuff just give it access and update in system dirtying when needed
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanGhostInteract
    {
        get => _canGhostInteract;
        set
        {
            if (_canGhostInteract == value) return;
            _canGhostInteract = value;
            Dirty();
        }
    }

    [DataField("canInteract"), AutoNetworkedField]
    private bool _canGhostInteract;

    /// <summary>
    ///     Changed by <see cref="SharedGhostSystem.SetCanReturnToBody"/>
    /// </summary>
    // TODO MIRROR change this to use friend classes when thats merged
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanReturnToBody
    {
        get => _canReturnToBody;
        set
        {
            if (_canReturnToBody == value) return;
            _canReturnToBody = value;
            Dirty();
        }
    }

    /// <summary>
    /// Ghost color
    /// </summary>
    /// <remarks>Used to allow admins to change ghost colors. Should be removed if the capability to edit existing sprite colors is ever added back.</remarks>
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    [DataField("canReturnToBody"), AutoNetworkedField]
    private bool _canReturnToBody;
}

public sealed partial class ToggleFoVActionEvent : InstantActionEvent { }

public sealed partial class ToggleGhostsActionEvent : InstantActionEvent { }

public sealed partial class ToggleLightingActionEvent : InstantActionEvent { }

public sealed partial class ToggleGhostHearingActionEvent : InstantActionEvent { }

public sealed partial class ToggleGhostVisibilityToAllEvent : InstantActionEvent { }

public sealed partial class BooActionEvent : InstantActionEvent { }

public sealed partial class RespawnActionEvent : InstantActionEvent { } //SS-220 noDeath

public sealed partial class ToggleAGhostBodyVisualsActionEvent : InstantActionEvent { } //SS220-ghost-hats

public sealed partial class ToggleHudOnOtherActionEvent : InstantActionEvent { } //SS220 ADD GHOST HUD'S
