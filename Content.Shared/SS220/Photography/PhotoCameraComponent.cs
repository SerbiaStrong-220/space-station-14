// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Photography;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PhotoCameraComponent : Component
{
    /// <summary>
    /// How many film charges can be loaded into camera
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint MaxFilm = 10;

    /// <summary>
    /// How many film charges left in camera (How many photos can be made until reloading is required)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public uint FilmLeft = 10;

    /// <summary>
    /// Dimensions of a photo, in tiles. X is Width, Y is Height.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SelectedPhotoDimensions = 5;

    /// <summary>
    /// Available dimensions of a photo, can be switched via context menu of a camera.
    /// </summary>
    [DataField]
    public float[] AvailablePhotoDimensions = { 3, 5, 7 };

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PrintingTime = TimeSpan.FromSeconds(3);

    [DataField]
    public EntProtoId PhotoPrototypeId = "Photo";

    [DataField]
    public SoundSpecifier ShotSound = new SoundPathSpecifier("/Audio/SS220/Items/polaroid.ogg");

    /// <summary>
    /// The time photo was taken at, is used to find out whether camera finished printing or not
    /// </summary>
    public TimeSpan? TimePhotoTakenAt;
}
