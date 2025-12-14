// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Photocopier;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Signature;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SignatureComponent : Component, IPhotocopyableComponent
{
    [DataField, AutoNetworkedField]
    public SignatureData? Data;

    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new SignaturePhotocopiedData
        {
            SignatureData = Data,
        };
    }
}

[Serializable]
public sealed class SignaturePhotocopiedData : IPhotocopiedComponentData
{
    public SignatureData? SignatureData;

    public bool NeedToEnsure => true;

    public void RestoreFromData(EntityUid uid, Component someComponent)
    {
        if (someComponent is not SignatureComponent signature)
            return;

        if (SignatureData == null)
            return;

        var entMan = IoCManager.Resolve<IEntityManager>();

        signature.Data = SignatureData;
        entMan.Dirty(uid, signature);
    }
}
