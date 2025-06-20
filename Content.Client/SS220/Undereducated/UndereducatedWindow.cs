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
        Resizable = false;
        Title = "Настройка малограмотного акцента";

        var vbox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            MinSize = new Vector2(350, 175),
            MaxSize = new Vector2(350, 175),
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

        vbox.AddChild(new Label { Text = " " });

        var percentLable = new Label();
        percentLable.Text = "Ваш уровень знания других языков : " + "95" + "%";
        vbox.AddChild(percentLable);
        _chanceSlider = new Slider
        {
            MinValue = 0,
            MaxValue = 95,
            Value = 95,
            HorizontalExpand = true,
            ToolTip = "Меньшее значение - больше автозамен слов",
            TooltipDelay = 0.9f
        };
        _chanceSlider.OnValueChanged += args =>
        {
            percentLable.Text = "Ваш уровень знания других языков : " + Math.Round(_chanceSlider.Value, 2) + "%";
            SelectedChance = 1f - args.Value / 100;
        };
        vbox.AddChild(_chanceSlider);

        vbox.AddChild(new Label
        {
            Text = " ",
            ToolTip = "Мяу?",
            TooltipDelay = 10
        });

        _submitButton = new Button
        {
            Text = "Применить",
            ToolTip = "Или просто закройте окно, все значения сохраняются",
            TooltipDelay = 0.9f
        };
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
