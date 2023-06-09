using Content.Client.Examine;
using Content.Client.Storage.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.Verbs.UI;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Content.Shared.SS220.CryopodSSD;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using static Content.Shared.SS220.CryopodSSD.SharedCryopodSSDSystem;

namespace Content.Client.SS220.CryopodSSD;

public sealed class CryopodSSDBoundUserInterface : BoundUserInterface
{
    private CryopodSSDWindow? _window = default!;
    private StorageWindow? _storageWindow = default!;

    public CryopodSSDBoundUserInterface(ClientUserInterfaceComponent owner, Enum uikey) : base(owner, uikey)
    {}
    
    protected override void Open()
    {
        base.Open();

        var entMan = IoCManager.Resolve<IEntityManager>();
        
        _window = new CryopodSSDWindow();
        _window.OnClose += Close;
        
        _window.OpenCentered();

        if (_storageWindow == null)
        {
            _storageWindow = new StorageWindow(entMan)
                {Title = entMan.GetComponent<MetaDataComponent>(Owner.Owner).EntityName};

            _storageWindow.EntityList.GenerateItem += _storageWindow.GenerateButton;
            _storageWindow.EntityList.ItemPressed += InteractWithItem;
            
            _storageWindow.OnClose += Close;
            _storageWindow.OpenCenteredLeft();
        }
        else
        {
            _storageWindow.Open();
        }
    }
    
    public void InteractWithItem(BaseButton.ButtonEventArgs args, ListData cData)
    {
        if (cData is not EntityListData {Uid: var entity})
            return;
        
        if (args.Event.Function == EngineKeyFunctions.UIClick)
        {
            SendMessage(new CryopodSSDStorageInteractWithItemEvent(entity));
        }
        else if (IoCManager.Resolve<IEntityManager>().EntityExists(entity))
        {
            OnButtonPressed(args.Event, entity);
        }
    }
    
    private void OnButtonPressed(GUIBoundKeyEventArgs args, EntityUid entity)
    {
        var entitySys = IoCManager.Resolve<IEntitySystemManager>();

        if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            entitySys.GetEntitySystem<ExamineSystem>()
                .DoExamine(entity);
        }
        else
        {
            return;
        }

        args.Handle();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CryopodSSDState castedState)
        {
            return;
        }

        if (_storageWindow is not null)
        {
            _storageWindow.Visible = castedState.HasAccess;
        }

        _storageWindow?.BuildEntityList(castedState.StorageState);
        _window?.UpdateState(castedState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            return;
        }

        if (_storageWindow is not null)
        {
            _storageWindow.EntityList.GenerateItem -= _storageWindow.GenerateButton;
            _storageWindow.EntityList.ItemPressed -= InteractWithItem;
            _storageWindow.OnClose -= Close;
        }

        _storageWindow?.Dispose();
        _storageWindow = null;
        _window?.Close();
    }
}