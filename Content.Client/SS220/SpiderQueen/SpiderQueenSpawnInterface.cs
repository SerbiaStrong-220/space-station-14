using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.SpiderQueen;

[UsedImplicitly]
public sealed partial class SpiderQueenSpawnInterface : BoundUserInterface
{
    private SpiderQueenSpawnWindow? _window;

    public SpiderQueenSpawnInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SpiderQueenSpawnWindow>();
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}
