// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.SS220.UserInterface;

[RegisterComponent]
public sealed partial class OnInteractUIComponent : Component
{
    [DataField(required: true, customTypeSerializer: typeof(EnumSerializer))]
    public Enum? Key;

    /// <summary>
    /// Whether the item must be held in one of the user's hands to work.
    /// This is ignored unless <see cref="RequiresComplex"/> is true.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool InHandsOnly;


    [DataField]
    public LocId VerbText = "ui-verb-toggle-open";
}
