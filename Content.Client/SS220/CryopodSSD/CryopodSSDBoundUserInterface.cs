using Robust.Client.GameObjects;
using Content.Shared.SS220.CryopodSSD;

namespace Content.Client.SS220.CryopodSSD;

public sealed class CryopodSSDBoundUserInterface : BoundUserInterface
{
    private CryopodSSDWindow? _window = default!;
    
    public CryopodSSDBoundUserInterface(ClientUserInterfaceComponent owner, Enum uikey) : base(owner, uikey)
    {}
    
    protected override void Open()
    {
        base.Open();

        _window = new CryopodSSDWindow();
        _window.OnClose += Close;
        
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CryopodSSDState castedState)
        {
            return;
        }

        _window?.UpdateState(castedState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        _window?.Close();
    }
}