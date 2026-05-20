using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordTradeModuleListHelpersTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: TradeModule ListHelpers
                target[LocalizationKeys.DiscordFeatureNotSetup] = "Sorry {0}, this bot does not have this feature set up.";
                target[LocalizationKeys.DiscordNoListMatches] = "{0} No {1} found matching the filter '{2}'.";
                target[LocalizationKeys.DiscordAvailableListTitle] = "Available {0} - Filter: '{1}'";
                target[LocalizationKeys.DiscordPageOf] = "Page {0} of {1}";
                target[LocalizationKeys.DiscordUseRequestCommand] = "Use `{0}{1} {2}` to request this {3}.";
                target[LocalizationKeys.DiscordDmSent] = "📩 {0}, I've sent you a DM with the list.";
                target[LocalizationKeys.DiscordDmBlocked] = "❌ {0}, I'm unable to send you a DM. Please check your **Server Privacy Settings**.";
                target[LocalizationKeys.DiscordDmError] = "❌ **Error**: Unable to send a DM. Please check your **Server Privacy Settings**.";
                target[LocalizationKeys.DiscordInvalidListIndex] = "{0} Invalid {1} index. Please use a valid number from the `.{2}` command.";
                target[LocalizationKeys.DiscordListConvertFailed] = "Failed to convert {0} file to the required PKM type.";
                target[LocalizationKeys.DiscordListRequestAdded] = "{0}, {1} request added to queue.";
                target[LocalizationKeys.DiscordGenericError] = "❌ An error occurred: {0}";
                break;
            case AppLanguage.Spanish:
                // Discord: TradeModule ListHelpers
                target[LocalizationKeys.DiscordFeatureNotSetup] = "Lo siento {0}, este bot no tiene esta funcion configurada.";
                target[LocalizationKeys.DiscordNoListMatches] = "{0} No se encontro {1} que coincida con el filtro '{2}'.";
                target[LocalizationKeys.DiscordAvailableListTitle] = "{0} disponibles - Filtro: '{1}'";
                target[LocalizationKeys.DiscordPageOf] = "Pagina {0} de {1}";
                target[LocalizationKeys.DiscordUseRequestCommand] = "Usa `{0}{1} {2}` para pedir este {3}.";
                target[LocalizationKeys.DiscordDmSent] = "📩 {0}, te envie la lista por DM.";
                target[LocalizationKeys.DiscordDmBlocked] = "❌ {0}, no puedo enviarte DM. Revisa tus **ajustes de privacidad del servidor**.";
                target[LocalizationKeys.DiscordDmError] = "❌ **Error**: no se pudo enviar DM. Revisa tus **ajustes de privacidad del servidor**.";
                target[LocalizationKeys.DiscordInvalidListIndex] = "{0} Indice de {1} invalido. Usa un numero valido del comando `.{2}`.";
                target[LocalizationKeys.DiscordListConvertFailed] = "No se pudo convertir el archivo de {0} al tipo PKM requerido.";
                target[LocalizationKeys.DiscordListRequestAdded] = "{0}, solicitud de {1} agregada a la cola.";
                target[LocalizationKeys.DiscordGenericError] = "❌ Ocurrio un error: {0}";
                break;
        }
    }
}
