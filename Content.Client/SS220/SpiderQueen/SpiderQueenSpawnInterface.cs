using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.SpiderQueenInterface;

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
}
