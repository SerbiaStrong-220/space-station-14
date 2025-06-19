// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Undereducated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Numerics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.SS220.Undereducated;

public sealed partial class UndereducatedWindow : DefaultWindow
{
    private List<string> _spokenLanguages;
    private OptionButton _languageOption = default!;
    private Slider _chanceSlider = default!;
    private Button _submitButton = default!;

    public string SelectedLanguage;
    public float SelectedChance;

    public UndereducatedWindow(UndereducatedComponent comp)
    {
        IoCManager.InjectDependencies(this);

        _spokenLanguages = comp.SpokenLanguages;
        SelectedLanguage = comp.Language;
        SelectedChance = comp.ChanseToReplace;

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

        var langIndex = _spokenLanguages.IndexOf(SelectedLanguage);
        if (langIndex >= 0)
        {
            _languageOption.Select(langIndex);
        }

        _languageOption.OnItemSelected += args =>
        {
            _languageOption.Select(args.Id);
            SelectedLanguage = _spokenLanguages[_languageOption.SelectedId];
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
        _chanceSlider.Value = SelectedChance;
        _chanceSlider.OnValueChanged += args =>
        {
            SelectedChance = 1f - args.Value / 100;
        };
        vbox.AddChild(_chanceSlider);

        _submitButton = new Button { Text = "Применить" };
        _submitButton.OnPressed += args =>
        {
            SelectedLanguage = _spokenLanguages[_languageOption.SelectedId];
            SelectedChance = 1f - _chanceSlider.Value / 100;
            Close();
        };
        vbox.AddChild(_submitButton);

        Contents.AddChild(vbox);
    }
}
