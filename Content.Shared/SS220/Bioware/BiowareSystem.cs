// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Bioware;

/// <summary>
/// Effects of the bioware: permanent surgical modifications of the body.
/// Each modification is a marker component on the body, so it stays with the patient when the organs are transplanted.
/// </summary>
public sealed partial class BiowareSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeCirculation();
        InitializeNerves();
    }
}
