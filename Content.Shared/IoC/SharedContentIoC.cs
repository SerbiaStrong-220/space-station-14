using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.SupaKitchen;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<MarkingManager, MarkingManager>();
            IoCManager.Register<ContentLocalizationManager, ContentLocalizationManager>();
            IoCManager.Register<LanguageManager>(); // SS220 languages
            IoCManager.Register<SupaRecipeManager, SupaRecipeManager>(); // SS220 Supa Kitchen
        }
    }
}
