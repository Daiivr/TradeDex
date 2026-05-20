using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordReusableActionsTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: ReusableActions
                target[LocalizationKeys.DiscordShowdownSetTitle] = "Pokemon Showdown Set";
                target[LocalizationKeys.DiscordShowdownSelfDestruct] = "This message will self-destruct in 15 seconds. Please copy your data.";
                break;
            case AppLanguage.Spanish:
                // Discord: ReusableActions
                target[LocalizationKeys.DiscordShowdownSetTitle] = "Pokemon Showdown Set";
                target[LocalizationKeys.DiscordShowdownSelfDestruct] = "Este mensaje se eliminara en 15 segundos. Copia tus datos.";
                break;
        }
    }
}
