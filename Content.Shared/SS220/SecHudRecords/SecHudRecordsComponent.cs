using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.SecHudRecords;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class SecHudRecordsComponent : Component
{
    [DataField(required: true)]
    public SpriteSpecifier VerbSprite;
}
