// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.Photography;

[RegisterComponent]
public sealed partial class PhotoFilmComponent : Component
{
    /// <summary>
    /// How many charges of film it adds to a camera when used
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Charges = 5;
}
