using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordBatchEditingModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: BatchEditingModule
                target[LocalizationKeys.DiscordBatchInfoResult] = "{0}: {1}";
                target[LocalizationKeys.DiscordBatchInfoNotFound] = "Unable to find info for {0}.";
                target[LocalizationKeys.DiscordBatchInvalidLines] = "Invalid Lines Detected:\r\n{0}";
                target[LocalizationKeys.DiscordBatchInvalidLineCount] = "{0} line(s) are invalid.";
                break;
            case AppLanguage.Spanish:
                // Discord: BatchEditingModule
                target[LocalizationKeys.DiscordBatchInfoResult] = "{0}: {1}";
                target[LocalizationKeys.DiscordBatchInfoNotFound] = "No se encontro informacion para {0}.";
                target[LocalizationKeys.DiscordBatchInvalidLines] = "Lineas invalidas detectadas:\r\n{0}";
                target[LocalizationKeys.DiscordBatchInvalidLineCount] = "{0} linea(s) son invalidas.";
                break;
        }
    }
}
