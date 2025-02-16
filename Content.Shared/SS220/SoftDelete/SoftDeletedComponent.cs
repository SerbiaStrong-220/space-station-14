// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.SoftDelete;

[RegisterComponent]
public sealed partial class SoftDeletedComponent : Component
{
    public EntityUid ActualParent;
}
