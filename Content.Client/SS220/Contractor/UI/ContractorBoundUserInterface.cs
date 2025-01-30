using System.Numerics;
using Content.Client.SS220.Contractor.Systems;
using Content.Client.SS220.CriminalRecords.UI;
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
            case ContractorUpdateStatsMessage:
                UpdateStats();
                UpdateContracts(EntMan.GetEntity(_contractorPdaComponent.PdaOwner)!.Value);
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

        var group = new ButtonGroup(false);

        _menu.ContractsButton.Group = group;
        _menu.HubButton.Group = group;

        UpdateStats();

        UpdateContracts(_playerManager.LocalSession.AttachedEntity.Value);
        UpdateHub(_contractorPdaComponent.AvailableItems);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_menu == null)
            return;

        if (state is not ContractorExecutionBoundUserInterfaceState cast)
            return;

        _menu.UpdateExecutionState(cast);
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

    private void UpdateContracts(EntityUid player)
    {
        if (_menu == null)
            return;

        _menu.ContractsListPanel.RemoveAllChildren();

        var contracts = _contractorComponent!.Contracts;

        if (_contractorPdaComponent.PdaOwner != EntMan.GetNetEntity(player))
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

        var contractContainer = new PanelContainer()
        {
            Margin = new Thickness(0),
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Top,
            StyleClasses = { "ContractsPanelFilled" },
        };

        var topContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Top,
        };

        var descriptionLabel = new RichTextLabel
        {
            Text = "Замечен в контакте с вульпой, что вызывает подозрения в утечке информации.",
            StyleClasses = { "ContractorRichLabelStyle" },
            HorizontalAlignment = Control.HAlignment.Left,
            Margin = new Thickness(2, 20, 170, 0),
            SetSize = new Vector2(137, 93),
            Modulate = Color.FromHex("#647b88"),
            RectClipContent = true,
            VerticalExpand = true,
        };

        contractContainer.AddChild(descriptionLabel);

        var nameLabel = new Label
        {
            Text = EntMan.GetComponent<MetaDataComponent>(EntMan.GetEntity(key)).EntityName + ", " + _prototypeManager.Index(contract.Job).LocalizedName,
            HorizontalExpand = false,
            MaxWidth = 350,
            Margin = new Thickness(5, 0, 0, 0),
            VerticalAlignment = Control.VAlignment.Center,
            StyleClasses = { "ContractorLabelStyle" }
        };

        var targetButton = new Button
        {
            HorizontalExpand = false,
            VerticalExpand = false,
            MinSize = new Vector2(32, 22),
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Control.VAlignment.Center,
            StyleClasses = { "ContractorExecutionButton" },
        };

        var iconTarget = new PanelContainer
        {
            MaxSize = new Vector2(16, 14),
            StyleClasses = { "ContractorPhotoImage" },
        };

        targetButton.AddChild(iconTarget);

        targetButton.OnPressed += _ =>
        {
            _photoWindow?.Close();

            _photoWindow = new DefaultWindow
            {
                Title = "Фото цели",
            };

            _contractorSystem.SpriteViewsForEntity.TryGetValue(EntMan.GetEntity(key), out var newTargetEntity);

            if (newTargetEntity == null)
                return;

            var iconTargetSprite = new CharacterVisualisation
            {
                SetSize = new Vector2(400, 300),
                Margin = new Thickness(100, 0, 0, 0),
            };

            iconTargetSprite.RemoveChild(1);
            iconTargetSprite.SetupCharacterSpriteView(newTargetEntity, _prototypeManager.Index(contract.Job).ID);

            _photoWindow.OnClose += () => _photoWindow = null;

            _photoWindow.AddChild(iconTargetSprite);
            _photoWindow.OpenCentered();
        };

        topContainer.AddChild(nameLabel);
        topContainer.AddChild(targetButton);

        var positionsContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Center,
            Margin = new Thickness(5, 30, 0, 0),
        };

        var isAlreadyAccepted = _contractorPdaComponent.CurrentContractEntity is not null &&
                                _contractorPdaComponent.CurrentContractData is not null; // todo only on server

        var abortButton = new Button
        {
            Text = "Отмена",
            VerticalExpand = false,
            HorizontalExpand = false,
            Visible = false,
            MaxSize = new Vector2(100, 30),
        };

        abortButton.OnPressed += _ =>
        {
            SendMessage(new ContractorAbortContractMessage(key));
            topContainer.RemoveChild(abortButton);
        };

        topContainer.AddChild(abortButton);

        foreach (var amountPosition in contract.AmountPositions)
        {
            if (_contractorPdaComponent.CurrentContractEntity == key)
            {
                abortButton.Visible = true;
            }

            var positionButton = new Button
            {
                HorizontalExpand = false,
                VerticalExpand = false,
                HorizontalAlignment = Control.HAlignment.Right,
                StyleClasses = { "ContractorExecutionButton" },
                SetSize = new Vector2(200, 30),
                Margin = new Thickness(0, 5, 0, 0),
                Disabled = isAlreadyAccepted,
            };

            var positionLabel = new Label
            {
                Text = $"{amountPosition.Location} ({amountPosition.TcReward} ТК)",
                HorizontalAlignment = Control.HAlignment.Center,
                VerticalAlignment = Control.VAlignment.Center,
                StyleClasses = { "ContractorLabelStyle" },
            };

            positionButton.AddChild(positionLabel);

            positionButton.OnPressed += _ =>
            {
                SendMessage(new ContractorNewContractAcceptedMessage(key,
                    contract,
                    amountPosition.TcReward,
                    amountPosition.Uid));

                abortButton.Visible = true;

                foreach (var buttons in _allPositionButtons)
                {
                    buttons.Disabled = true;
                }
            };


            _allPositionButtons.Add(positionButton);
            positionsContainer.AddChild(positionButton);
        }

        contractContainer.AddChild(topContainer);
        contractContainer.AddChild(positionsContainer);

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

        _menu.ContractorTopLabel.Text = "Доступные контракты";
        _menu.HubPanel.Visible = false;
        _menu.ContractsListPanel.Visible = true;

    }

    public void OnHubButtonPressed()
    {
        if (_menu == null)
            return;

        _menu.ContractorTopLabel.Text = "Доступные товары";
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
