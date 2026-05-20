using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordEmbedHelperTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: EmbedHelper
                target[LocalizationKeys.DiscordNoticeTitle] = "Notice";
                target[LocalizationKeys.DiscordTradeCanceledTitle] = "Your trade was canceled...";
                target[LocalizationKeys.DiscordTradeCanceledDescription] = "Your trade has been canceled.\nPlease try again. If the problem persists, restart your console and check your internet connection:\n\n**Reason**: {0}";
                target[LocalizationKeys.DiscordTradeCodeTitle] = "Added to the Queue!";
                target[LocalizationKeys.DiscordTradeCodeDescription] = "✅ I added you to the __list__! I'll send you a __message__ here when your trade begins...\n\nHere is your trade code!\n# {0:0000 0000}";
                target[LocalizationKeys.DiscordTradeCompletedTitle] = "Trade Completed!";
                target[LocalizationKeys.DiscordLoadingTradeMenuTitle] = "Loading the Poke Portal...";
                target[LocalizationKeys.DiscordTradeMenuDescription] = "**Trade**: {0}\n**Trade Code**: {1:0000 0000}";
                target[LocalizationKeys.DiscordNowSearchingTitle] = "Searching for Trainer...";
                target[LocalizationKeys.DiscordSearchingDescription] = "**Waiting for**: {0}\n**My IGN**: {1}";
                break;
            case AppLanguage.Spanish:
                // Discord: EmbedHelper
                target[LocalizationKeys.DiscordNoticeTitle] = "Aviso";
                target[LocalizationKeys.DiscordTradeCanceledTitle] = "Tu trade fue cancelado...";
                target[LocalizationKeys.DiscordTradeCanceledDescription] = "Tu trade fue cancelado.\nIntentalo de nuevo. Si el problema persiste, reinicia tu consola y revisa tu conexion a internet:\n\n**Razon**: {0}";
                target[LocalizationKeys.DiscordTradeCodeTitle] = "Agregado a la cola!";
                target[LocalizationKeys.DiscordTradeCodeDescription] = "✅ Te he agregado a la __lista__! Te enviare un __mensaje__ aqui cuando comience tu operacion...\n\nAqui esta tu codigo de trade!\n# {0:0000 0000}";
                target[LocalizationKeys.DiscordTradeCompletedTitle] = "Trade completado!";
                target[LocalizationKeys.DiscordLoadingTradeMenuTitle] = "Cargando el Pokeportal...";
                target[LocalizationKeys.DiscordTradeMenuDescription] = "**Intercambio**: {0}\n**Trade Code**: {1:0000 0000}";
                target[LocalizationKeys.DiscordNowSearchingTitle] = "Buscando entrenador...";
                target[LocalizationKeys.DiscordSearchingDescription] = "**Esperando por**: {0}\n**Mi IGN**: {1}";
                break;
        }
    }
}
