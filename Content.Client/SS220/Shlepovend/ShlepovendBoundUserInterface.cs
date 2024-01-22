// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.Shlepovend.UI;
using Content.Shared.SS220.Shlepovend;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Shlepovend;

public sealed class ShlepovendBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [ViewVariables]
    private ShlepovendWindow? _window;

    public ShlepovendBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new ShlepovendWindow();

        _window.OnClose += Close;
        _window.OnPurchase += OnPurchase;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }

    private void OnPurchase((ProtoId<ShlepaRewardGroupPrototype>, EntProtoId)? args)
    {
        if (!args.HasValue)
            return;

        SendMessage(new ShlepovendPurchaseMsg() { GroupId = args.Value.Item1, ItemId = args.Value.Item2 });
    }
}
