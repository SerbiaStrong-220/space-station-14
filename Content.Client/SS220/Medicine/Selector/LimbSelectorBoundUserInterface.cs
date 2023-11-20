// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Medicine.Surgery;
using JetBrains.Annotations;

namespace Content.Client.SS220.Medicine.Selector;

[UsedImplicitly]
public sealed partial class LimbSelectorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    public LimbSelectorLayout? _layout;

    public readonly EntityUid Entity; 
    public LimbSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();
        _layout = new LimbSelectorLayout();
        _layout.OnClose += Close;
        _layout.OpenCentered();

        _layout.OnContainedEntityButtonPressed += id =>
        {
            SendMessage(new SelectorButtonPressed(id));
        };
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is InstrumentUsedAfterInteractEvent msg)
        {
            _layout!.UpdatePanels(msg);
        }
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}