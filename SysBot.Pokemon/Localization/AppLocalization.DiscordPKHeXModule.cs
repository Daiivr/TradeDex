using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordPKHeXModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: PKHeXModule
                target[LocalizationKeys.DiscordPkhexDirectoryMissing] = "PKHeX directory is not set or does not exist. Please configure it in the Hub settings.";
                target[LocalizationKeys.DiscordPkhexExecutableMissing] = "No PKHeX executable found in the configured folder.";
                target[LocalizationKeys.DiscordPkhexLaunched] = "PKHeX launched successfully.";
                target[LocalizationKeys.DiscordPkhexLaunchFailed] = "Failed to launch PKHeX: {0}";
                break;
            case AppLanguage.Spanish:
                // Discord: PKHeXModule
                target[LocalizationKeys.DiscordPkhexDirectoryMissing] = "El directorio de PKHeX no esta configurado o no existe. Configuralo en los ajustes del Hub.";
                target[LocalizationKeys.DiscordPkhexExecutableMissing] = "No se encontro ningun ejecutable de PKHeX en la carpeta configurada.";
                target[LocalizationKeys.DiscordPkhexLaunched] = "PKHeX se inicio correctamente.";
                target[LocalizationKeys.DiscordPkhexLaunchFailed] = "No se pudo iniciar PKHeX: {0}";
                break;
        }
    }
}
