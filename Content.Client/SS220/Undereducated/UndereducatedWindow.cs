// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Undereducated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Numerics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.SS220.Undereducated;

public sealed partial class UndereducatedWindow : DefaultWindow
{
    private readonly UserInterfaceSystem _uiSystem;
    private readonly EntityUid _entity;
    private List<string> _spokenLanguages;

    [Dependency] private readonly IEntityManager _entManager = default!;

    private OptionButton _languageOption = default!;
    private Slider _chanceSlider = default!;
    private Button _submitButton = default!;

    private string _currentLanguage;
    private float _currentChance;

    public UndereducatedWindow(EntityUid owner, UndereducatedComponent comp)
    {
        IoCManager.InjectDependencies(this);
        _uiSystem = _entManager.System<UserInterfaceSystem>();
        _entity = owner;
        _spokenLanguages = comp.SpokenLanguages;

        _currentLanguage = comp.Language;
        _currentChance = comp.ChanseToReplace;

        BuildWindow();
    }

    private void BuildWindow()
    {
        Title = "Настройка акцента";

        var vbox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            MinSize = new Vector2(300, 150)
        };

        vbox.AddChild(new Label { Text = "Ваш родной язык:" });
        _languageOption = new OptionButton();
        foreach (var language in _spokenLanguages)
        {
            _languageOption.AddItem(language);
        }

        var langIndex = _spokenLanguages.IndexOf(_currentLanguage);
        if (langIndex >= 0)
        {
            _languageOption.Select(langIndex);
        }

        _languageOption.OnItemSelected += args =>
        {
            _languageOption.Select(args.Id);
        };

        vbox.AddChild(_languageOption);

        vbox.AddChild(new Label { Text = "Ваш уровень знания других языков (%):" });
        _chanceSlider = new Slider
        {
            MinValue = 5,
            MaxValue = 100,
            Value = 5,
            HorizontalExpand = true
        };
        _chanceSlider.Value = _currentChance;
        vbox.AddChild(_chanceSlider);

        _submitButton = new Button { Text = "Применить" };
        _submitButton.OnPressed += OnSubmit;
        vbox.AddChild(_submitButton);

        Contents.AddChild(vbox);
    }

    private void OnSubmit(BaseButton.ButtonEventArgs args)
    {
        if (_languageOption.SelectedId < 0 || _languageOption.SelectedId >= _spokenLanguages.Count)
            return;
        var selectedLanguage = _spokenLanguages[_languageOption.SelectedId];

        float chance = 1f - _chanceSlider.Value / 100;

        var message = new UndereducatedConfigRequest(selectedLanguage, chance);
        _uiSystem.ClientSendUiMessage(_entity, UndereducatedUiKey.Key, message);
        Close();
    }

}
