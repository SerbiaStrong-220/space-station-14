using Content.Shared.SS220.LocateAi;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.LocateAi;

public sealed class LocateAiSystem : SharedLocateAiSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<LocateAiEvent>(OnLocateAi);
    }

    private void OnLocateAi(LocateAiEvent args)
    {
        if (!HasComp<AppearanceComponent>(GetEntity(args.Tool)))
            return;

        _appearance.SetData(GetEntity(args.Tool), LocateAiVisuals.Visuals, args.IsNear);
    }
}

public enum LocateAiVisuals : byte
{
    Visuals,
}
