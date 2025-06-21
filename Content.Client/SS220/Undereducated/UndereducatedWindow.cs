// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Undereducated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Numerics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.SS220.Undereducated;

public sealed partial class UndereducatedWindow : DefaultWindow
{
    private readonly List<string> _spokenLanguages;
    private OptionButton _languageOption = default!;
    private Slider _chanceSlider = default!;
    private Button _submitButton = default!;
    private int _secret = 0;

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
        Title = Loc.GetString("window-undereducated-title");

        var vbox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            MinSize = new Vector2(350, 175),
            MaxSize = new Vector2(350, 175),
        };

        var langLable = new Label { Text = Loc.GetString("window-undereducated-language-option-label") };
        vbox.AddChild(langLable);
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
            _secret++;
            if (_secret == 30)
                langLable.Text = $"{Loc.GetString("window-undereducated-language-option-label")} {Loc.GetString("window-undereducated-language-option-label-secret")}";
        };
        vbox.AddChild(_languageOption);

        vbox.AddChild(new Label { Text = " " });

        var percentLable = new Label();
        percentLable.Text = $"{Loc.GetString("window-undereducated-percent-slider-label")} 95%";
        vbox.AddChild(percentLable);
        _chanceSlider = new Slider
        {
            MinValue = 0,
            MaxValue = 95,
            Value = 95,
            HorizontalExpand = true,
            ToolTip = Loc.GetString("window-undereducated-percent-slider-tooltip"),
            TooltipDelay = 0.9f
        };
        _chanceSlider.OnValueChanged += args =>
        {
            percentLable.Text = $"{Loc.GetString("window-undereducated-percent-slider-label")} {Math.Round(_chanceSlider.Value, 2)}%";
            SelectedChance = 1f - args.Value / 100;
        };
        vbox.AddChild(_chanceSlider);

        vbox.AddChild(new Label { Text = " " });

        _submitButton = new Button
        {
            Text = Loc.GetString("window-undereducated-confirm-button-text"),
            ToolTip = Loc.GetString("window-undereducated-confirm-button-tooltip"),
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
