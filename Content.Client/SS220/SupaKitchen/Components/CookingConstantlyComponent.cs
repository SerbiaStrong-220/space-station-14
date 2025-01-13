// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SupaKitchen.Components;

namespace Content.Client.SS220.SupaKitchen.Components;

[RegisterComponent]
public sealed partial class CookingConstantlyComponent : SharedCookingConstantlyComponent
{
    [DataField]
    public string ActiveState = "oven_on";
    [DataField]
    public string NonActiveState = "oven_off";
}
