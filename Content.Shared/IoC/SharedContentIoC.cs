using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;
using Content.Shared.SS220.Language;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register(IDependencyCollection deps)
        {
            deps.Register<LanguageManager>(); // SS220 languages
            deps.Register<MarkingManager, MarkingManager>();
            deps.Register<ContentLocalizationManager, ContentLocalizationManager>();
        }
    }
}
