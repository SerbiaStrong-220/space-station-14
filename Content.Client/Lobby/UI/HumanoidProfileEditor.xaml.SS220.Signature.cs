using Content.Shared.SS220.Signature;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    // this is here cause I CAN
    private void SetTeleportAfkToCryoStorage(bool newTeleportAfkToCryoStorage)
    {
        Profile = Profile?.WithTeleportAfkToCryoStorage(newTeleportAfkToCryoStorage);
        SetDirty();
    }

    private void UpdateSignature()
    {
        if (Profile == null)
            return;

        Signature.SetSignature(Profile.SignatureData);
    }

    private void SetSignatureData(SignatureData? newSignatureData)
    {
        if (newSignatureData is null)
            return;

        Profile = Profile?.WithSignatureData(newSignatureData.Clone());
        SetDirty();
    }
}
