using System.Numerics;
using Content.Client.SS220.Contractor.Systems;
using Content.Client.SS220.UserInterface;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Contractor;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Contractor.UI;

[UsedImplicitly]
public sealed class ContractorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ContractorPDAMenu? _menu;

    private DefaultWindow? _withdrawWindow;
    private DefaultWindow? _photoWindow;

    private readonly ContractorClientSystem _contractorSystem;
    private readonly ContractorPdaComponent _contractorPdaComponent;
    private readonly ContractorComponent? _contractorComponent;

    private readonly List<Button> _allPositionButtons = [];

    public ContractorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _contractorSystem = EntMan.System<ContractorClientSystem>();
        _contractorPdaComponent = EntMan.GetComponent<ContractorPdaComponent>(owner);
        EntMan.TryGetComponent<ContractorComponent>(
            EntMan.GetEntity(_contractorPdaComponent.PdaOwner),
            out var contractorComponent);

        _contractorComponent = contractorComponent;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if(_menu == null)
            return;

        switch (message)
        {
            case ContractorUpdateButtonStateMessage ev:
                _menu.ExecutionButton.Disabled = !ev.IsEnabled;
                break;
            case ContractorUpdateStatsMessage:
                UpdateStats();
                UpdateContracts(EntMan.GetEntity(_contractorPdaComponent.PdaOwner)!.Value, Owner);
                break;
            case ContractorCompletedContractMessage:
            {
                foreach (var buttons in _allPositionButtons)
                {
                    buttons.Disabled = false;
                }
                _menu.ExecutionButton.Disabled = true;
                break;
            }
        }
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(this);
        _menu.OnClose += Close;

        _menu.OpenCentered();

        if (_playerManager.LocalSession?.AttachedEntity == null)
            return;

        if (_contractorPdaComponent.PdaOwner == null)
            return;

        UpdateStats();

        UpdateContracts(_playerManager.LocalSession.AttachedEntity.Value, Owner);
        UpdateHub(_contractorPdaComponent.AvailableItems);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    private void UpdateStats()
    {
        if (_menu == null)
            return;

        if (_contractorComponent == null)
            return;

        _menu.ReputationCountLabel.Text =
            Loc.GetString("contractor-uplink-current-reputation", ("currentReputation", _contractorComponent.Reputation));
        _menu.TcAmountLabel.Text =
            Loc.GetString("contractor-uplink-current-tc", ("amountTc", _contractorComponent.AmountTc));
        _menu.ContractsFinishedAmountLabel.Text =
            Loc.GetString("contractor-uplink-current-contracts-completed", ("amountContractsCompleted", _contractorComponent.ContractsCompleted));
    }

    private void UpdateContracts(EntityUid contractor, EntityUid pda)
    {
        if (_menu == null)
            return;

        _menu.ContractsListPanel.RemoveAllChildren();

        var contracts = _contractorSystem.GetContractsForPda(contractor, pda);

        if (contracts is null)
            return;

        foreach (var contract in contracts)
        {
            AddContract(contract.Key, contract.Value);
        }

    }

    private void AddContract(NetEntity key, ContractorContract contract)
    {
        if (_menu == null)
            return;

        var contractContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Top,
            Margin = new Thickness(5, 20, 5, 60),
        };

        var topContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Top,
        };

        var nameLabel = new RichTextLabel
        {
            Text = EntMan.GetComponent<MetaDataComponent>(EntMan.GetEntity(key)).EntityName + " - " + contract.Job,
            HorizontalExpand = false,
            MaxWidth = 350,
            VerticalAlignment = Control.VAlignment.Center,
        };

        var targetButton = new SpriteButton
        {
            HorizontalExpand = false,
            VerticalExpand = false,
            MinSize = new Vector2(28, 28),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = Control.VAlignment.Center,
            StyleClasses = { "ContractorPhotoButton" },
        };

        targetButton.OnPressed += _ =>
        {
            _photoWindow?.Close();

            _photoWindow = new DefaultWindow
            {
                Title = "Фото цели",
                MinSize = new Vector2(200, 200),
                Resizable = false,
            };

            _photoWindow.OnClose += () => _photoWindow = null;

            var iconTarget = new EntityPrototypeView
            {
                Scale = new Vector2(1.5f, 1.5f),
                VerticalAlignment = Control.VAlignment.Center,
                HorizontalAlignment = Control.HAlignment.Center,
            };

            iconTarget.SetEntity(EntMan.GetEntity(key));

            _photoWindow.AddChild(iconTarget);
            _photoWindow.OpenCentered();
        };

        topContainer.AddChild(nameLabel);
        topContainer.AddChild(targetButton);

        var positionsContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 10,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Center,
            Margin = new Thickness(5, 5, 5, 5),
        };

        var isAlreadyAccepted = _contractorPdaComponent.CurrentContractEntity is not null &&
                                _contractorPdaComponent.CurrentContractData is not null; // todo only on server

        foreach (var amountPosition in contract.AmountPositions)
        {
            var positionButton = new Button
            {
                Text = $"{amountPosition.Location} ({amountPosition.TcReward} ТК) ({amountPosition.Difficulty})",
                HorizontalExpand = false,
                VerticalExpand = false,
                StyleClasses = { "OpenBoth" },
                MinSize = new Vector2(200, 30),
                Disabled = isAlreadyAccepted,
            };

            positionButton.OnPressed += _ =>
            {
                SendMessage(new ContractorNewContractAcceptedMessage(key,
                    contract,
                    amountPosition.TcReward,
                    amountPosition.Uid));

                foreach (var buttons in _allPositionButtons)
                {
                    buttons.Disabled = true;
                }
            };


            _allPositionButtons.Add(positionButton);
            positionsContainer.AddChild(positionButton);
        }

        var lowerDivider = new PanelContainer
        {
            StyleClasses = { "HighDivider" },
        };

        contractContainer.AddChild(topContainer);
        contractContainer.AddChild(positionsContainer);


        _menu.ContractsListPanel.AddChild(lowerDivider);
        _menu.ContractsListPanel.AddChild(contractContainer);
    }

    private void UpdateHub(Dictionary<string, FixedPoint2> shopItems)
    {
        if (_menu == null)
            return;

        _menu.HubPanel.RemoveAllChildren();

        foreach (var item in shopItems)
        {
            var protoItem = _prototypeManager.Index<EntityPrototype>(item.Key);

            var shopItemContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 5,
                HorizontalExpand = true,
                VerticalExpand = true,
                VerticalAlignment = Control.VAlignment.Top,
                Margin = new Thickness(5, 5, 5, 5),
            };

            var topRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 10,
                HorizontalExpand = true,
                VerticalAlignment = Control.VAlignment.Center,
            };

            var itemIcon = new EntityPrototypeView
            {
                Scale = new Vector2(1.4f, 1.4f),
                MinSize = new Vector2(32, 32),
                MaxSize = new Vector2(50, 50),
                VerticalAlignment = Control.VAlignment.Center,
                HorizontalAlignment = Control.HAlignment.Left,
            };

            itemIcon.SetPrototype(protoItem);

            var itemNameLabel = new RichTextLabel
            {
                Text = protoItem.Name,
                HorizontalExpand = true,
                VerticalAlignment = Control.VAlignment.Center,
            };

            var buyButton = new Button
            {
                Text = $"Купить ({item.Value} Реп.)",
                HorizontalExpand = false,
                VerticalExpand = false,
                StyleClasses = { "OpenBoth" },
                MinSize = new Vector2(120, 30),
            };

            buyButton.OnPressed += _ =>
            {
                SendMessage(new ContractorHubBuyItemMessage(item.Key, item.Value));

                //if (_contractorComponent is not null)
                    //UpdateStats(_contractorComponent);


            };

            topRow.AddChild(itemIcon);
            topRow.AddChild(itemNameLabel);
            topRow.AddChild(buyButton);

            var itemDescriptionLabel = new RichTextLabel
            {
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            itemDescriptionLabel.SetMessage(protoItem.Description);

            var descriptionContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            descriptionContainer.AddChild(itemDescriptionLabel);

            shopItemContainer.AddChild(topRow);
            shopItemContainer.AddChild(descriptionContainer);

            var lowerDivider = new PanelContainer
            {
                StyleClasses = { "HighDivider" },
            };

            _menu.HubPanel.AddChild(shopItemContainer);
            _menu.HubPanel.AddChild(lowerDivider);
        }
    }

    public void OnContractsButtonPressed()
    {
        if (_menu == null)
            return;

        _menu.HubPanel.Visible = false;
        _menu.ContractsListPanel.Visible = true;

    }

    public void OnHubButtonPressed()
    {
        if (_menu == null)
            return;

        _menu.HubPanel.Visible = true;
        _menu.ContractsListPanel.Visible = false;
    }

    public void OnExecutionButtonPressed()
    {
        if (_menu == null)
            return;

        if (_contractorComponent?.CurrentContractEntity is null)
            return;

        if (_contractorComponent?.CurrentContractData is null)
            return;

        SendMessage(new ContractorExecutionButtonPressedMessage());

    }

    public void OnWithdrawButtonPressed()
    {
        if (_menu == null)
            return;

        if (_contractorComponent == null || _contractorComponent.AmountTc <= 0)
            return;

        if (EntMan.GetEntity(_contractorPdaComponent.PdaOwner) != PlayerManager.LocalEntity)
            return;

        _withdrawWindow?.Close();

        _withdrawWindow = new DefaultWindow
        {
            Title = "Вывести",
            MinSize = new Vector2(300, 150),
            Resizable = false,
        };

        _withdrawWindow.OnClose += () => _withdrawWindow = null;


        var mainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 10,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(10),
        };

        var tcSlider = new SliderIntInput
        {
            MinValue = 1,
            MaxValue = (int)_contractorComponent.AmountTc,
            HorizontalExpand = true,
            Value = 1,
        };

        var withdrawButton = new Button
        {
            Text = "Вывести ТК",
            HorizontalExpand = true,
            MinSize = new Vector2(200, 40),
            StyleClasses = { "OpenBoth" },
        };

        withdrawButton.OnPressed += _ =>
        {
            var amount = tcSlider.Value;

            SendMessage(new ContractorWithdrawTcMessage(amount));
            _withdrawWindow.Close();
        };

        mainContainer.AddChild(tcSlider);
        mainContainer.AddChild(withdrawButton);

        _withdrawWindow.Contents.AddChild(mainContainer);
        _withdrawWindow.OpenCentered();
    }


}
