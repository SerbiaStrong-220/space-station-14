using Content.Shared.SS220.Shlepovend;

namespace Content.Client.SS220.Shlepovend;

public sealed class ShlepovendSystem : SharedShlepovendSystem
{
    public int Tokens { get; private set; } = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShlepovendTokenAmountMsg>(OnTokenAmountMsg);
    }

    private void OnTokenAmountMsg(ShlepovendTokenAmountMsg args)
    {
        Tokens = args.Tokens;
    }
}
