// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.UserInterface;

namespace Content.Client.SS220.StyleTools
{
    public sealed class StylesheetBuilder
    {
        private readonly List<StyleRule> _rules;

        private MutableSelectorElement? _currentMainSelector;
        private MutableSelectorElement? _currentChildSelector;
        private MutableSelectorElement? _currentSelector;

        public StylesheetBuilder() : this(new()) { }

        private StylesheetBuilder(List<StyleRule> rules)
        {
            _rules = rules;
        }

        public static StylesheetBuilder FromBase(Stylesheet? stylesheet)
        {
            if (stylesheet is null)
                return new StylesheetBuilder();
            return new StylesheetBuilder(new(stylesheet.Rules.Count)).Inherit(stylesheet);
        }

        public StylesheetBuilder Inherit(Stylesheet stylesheet)
        {
            for (var i = 0; i < stylesheet.Rules.Count; i++)
            {
                _rules.Add(stylesheet.Rules[i]);
            }
            return this;
        }

        public StylesheetBuilder Element<T>() where T : Control
        {
            EndCurrentRule();
            _currentMainSelector = StylesheetHelpers.Element<T>();
            _currentSelector = _currentMainSelector;
            return this;
        }

        public StylesheetBuilder Child<T>() where T : Control
        {
            if (_currentMainSelector is null)
                throw new InvalidOperationException("Parent element should be selected first");
            if (_currentChildSelector is not null)
                throw new InvalidOperationException("Chained child selectors is not supported");
            _currentChildSelector = StylesheetHelpers.Element<T>();
            _currentSelector = _currentChildSelector;
            return this;
        }

        public StylesheetBuilder Class(string className)
        {
            _currentSelector?.Class(className);
            return this;
        }

        public StylesheetBuilder Pseudo(string name)
        {
            _currentSelector?.Pseudo(name);
            return this;
        }

        public StylesheetBuilder Prop(string key, object value)
        {
            _currentSelector?.Prop(key, value);
            return this;
        }

        public Stylesheet Build()
        {
            EndCurrentRule();
            return new Stylesheet(_rules);
        }

        private void EndCurrentRule()
        {
            if (_currentMainSelector is null)
                return;
            if (_currentChildSelector is null)
            {
                _rules.Add(_currentMainSelector);
            }
            else
            {
                _rules.Add(StylesheetHelpers.Child().Parent(_currentMainSelector).Child(_currentChildSelector));
            }
            _currentMainSelector = null;
            _currentChildSelector = null;
        }
    }
}
