// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Medicine.Injury.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Medicine.Surgery.Systems;

public sealed partial class SharedSurgerySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

}

[Serializable, NetSerializable]
public enum LimbSelectorUiKey : byte
{
    Key
}