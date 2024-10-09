using Content.Shared.SS220.FastUI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.FastUI;

[UsedImplicitly]
public sealed class SecretListingBoundUserInterface : BoundUserInterface
{
    private SecretListingWindow? _listingWindow;
    private string? _key;
    private NetEntity? _user;

    public SecretListingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _listingWindow = this.CreateWindow<SecretListingWindow>();
        _listingWindow.OpenCenteredLeft();
        _listingWindow.OnItemSelected += OnItemSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_listingWindow == null)
            return;

        if (state is SecretListingInitDataState initDataState)
        {
            _listingWindow.WindowDescription.Text = initDataState.WindowDescription;
            _listingWindow.Title = initDataState.WindowName;
            _user = initDataState.UserEntity;
            _key = initDataState.Key;

            _listingWindow.Populate(initDataState.Data);
        }
        else if (state is SecretListingInitState initState)
        {
            _listingWindow.WindowDescription.Text = initState.Prototype.WindowDescription;
            _listingWindow.Title = initState.Prototype.WindowName;
            _user = initState.UserEntity;
            _key = initState.Prototype.ID;

            _listingWindow.Populate(initState.Prototype.Listings);
        }
    }

    private void OnItemSelected(ListingData data)
    {
        if (_key == null || _user == null)
            return;

        SendMessage(new SelectItemMessage(_key, data, _user.Value));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_listingWindow == null)
            return;

        _listingWindow.OnItemSelected -= OnItemSelected;
    }
}
