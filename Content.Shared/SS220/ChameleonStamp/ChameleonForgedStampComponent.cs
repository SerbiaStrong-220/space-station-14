// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Photocopier;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ChameleonStamp;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class ChameleonForgedStampComponent : Component, IPhotocopyableComponent
{
    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new ChameleonForgedStampPhotocopiedData();
    }
}

[Serializable]
public sealed class ChameleonForgedStampPhotocopiedData : IPhotocopiedComponentData
{
    public bool NeedToEnsure => true;

    public void RestoreFromData(EntityUid uid, Component someComponent)
    {
    }
}
