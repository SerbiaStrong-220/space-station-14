// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Shared.SS220.Signature;

namespace Content.Client.SS220.Signature;

public sealed class SignatureSystem : SharedSignatureSystem
{
    [Dependency] private readonly IClientAdminManager _admin = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SendSignatureToAdminEvent>(OnSignature);
    }

    private void OnSignature(SendSignatureToAdminEvent ev)
    {
        if (!_admin.IsAdmin())
            return;

        var canvasSize = new Vector2(ev.Data.Width, ev.Data.Height);
        var window = new SignatureWindow(canvasSize);
        window.Signature.SetSignature(ev.Data);

        window.OpenCentered();
    }
}
