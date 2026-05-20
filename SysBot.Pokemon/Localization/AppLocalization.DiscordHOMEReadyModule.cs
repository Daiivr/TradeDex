using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordHOMEReadyModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: HOMEReadyModule
                target[LocalizationKeys.DiscordHomeReadyInstructionsTitle] = "------- HOME-READY MODULE INSTRUCTIONS -------";
                target[LocalizationKeys.DiscordHomeReadyInstructionsDescription] = "Everything you need to know for the HOME-Ready commands.";
                target[LocalizationKeys.DiscordHomeReadyGetListTitle] = "GET LIST - `{0}hrl <Pokemon>`";
                target[LocalizationKeys.DiscordHomeReadyGetListDescription] = "- Searches the entire HOME-Ready module.\n**Example:** `{0}hrl Mewtwo`";
                target[LocalizationKeys.DiscordHomeReadyChangePagesTitle] = "CHANGE PAGES - `{0}hrl <page>`";
                target[LocalizationKeys.DiscordHomeReadyChangePagesDescription] = "- Switch between pages, with or without filters.\n**Example:** `{0}hrl 5 Charmander`";
                target[LocalizationKeys.DiscordHomeReadyTradeFileTitle] = "TRADE A FILE - `{0}hrr <number>`";
                target[LocalizationKeys.DiscordHomeReadyTradeFileDescription] = "- Trades the Pokemon by its number in the list.\n**Example:** `{0}hrr 682`";
                target[LocalizationKeys.DiscordHomeReadyNotConfigured] = "This bot does not have the HOME-Ready module configured.";
                target[LocalizationKeys.DiscordHomeReadyAlreadyInQueue] = "You're already in a trade queue. Finish that one first.";
                target[LocalizationKeys.DiscordHomeReadyNoPkmFiles] = "No HOME-ready PKM files found.";
                target[LocalizationKeys.DiscordHomeReadyNoFiles] = "No HOME-ready files found.";
                target[LocalizationKeys.DiscordHomeReadyInvalidEntry] = "Invalid entry number. Choose a valid file.";
                target[LocalizationKeys.DiscordHomeReadyConvertInvalid] = "Could not convert the file to a valid PKM.";
                target[LocalizationKeys.DiscordHomeReadyConvertTradeFormatFailed] = "Failed to convert the HOME-ready file to your trade format.";
                target[LocalizationKeys.DiscordHomeReadyAddedQueue] = "**HOME-Ready Pokemon added to the queue.**";
                target[LocalizationKeys.DiscordHomeReadyNoMatches] = "No HOME-ready files found for '{0}'.";
                target[LocalizationKeys.DiscordHomeReadyListTitle] = "HOME-Ready Files - '{0}'";
                target[LocalizationKeys.DiscordHomeReadyListDescription] = "Page **{0}** of **{1}**";
                target[LocalizationKeys.DiscordHomeReadyListField] = "Use `{0}hrr {1}` to request this Pokemon.\nUse `{0}hrv {1}` to view Pokemon details.\nUse `{0}hrd {1}` to download this PKM file.\nThis file is for **{2}**.";
                target[LocalizationKeys.DiscordHomeReadyReadFailed] = "Could not read the PKM file.";
                target[LocalizationKeys.DiscordHomeReadyLoadedConvertFailed] = "File loaded, but could not convert to your game generation.";
                target[LocalizationKeys.DiscordHomeReadyTrackerEmpty] = "N/A - Just trade with a **{0}** bot.";
                target[LocalizationKeys.DiscordHomeReadyAdditionalDetails] = "**Additional Details**\nâ€¢ OT: {0}\nâ€¢ TID: {1}\nâ€¢ Game: {2}\nâ€¢ Met Location: {3} ({4})\nâ€¢ Met Date: {5}\nâ€¢ Tracker: {6}";
                target[LocalizationKeys.DiscordHomeReadyViewTitle] = "Viewing HOME-Ready Entry #{0}";
                target[LocalizationKeys.DiscordHomeReadyViewFooter] = "Use {0}hrr {1} to request this Pokemon for trade.\nUse {0}hrd {1} to download this PKM file.";
                target[LocalizationKeys.DiscordHomeReadyDownloadText] = "Here's your HOME-Ready Pokemon file: **{0}**";
                break;
            case AppLanguage.Spanish:
                // Discord: HOMEReadyModule
                target[LocalizationKeys.DiscordHomeReadyInstructionsTitle] = "------- INSTRUCCIONES DEL MODULO HOME-READY -------";
                target[LocalizationKeys.DiscordHomeReadyInstructionsDescription] = "Todo lo que necesitas saber para los comandos HOME-Ready.";
                target[LocalizationKeys.DiscordHomeReadyGetListTitle] = "VER LISTA - `{0}hrl <Pokemon>`";
                target[LocalizationKeys.DiscordHomeReadyGetListDescription] = "- Busca en todo el modulo HOME-Ready.\n**Ejemplo:** `{0}hrl Mewtwo`";
                target[LocalizationKeys.DiscordHomeReadyChangePagesTitle] = "CAMBIAR PAGINAS - `{0}hrl <pagina>`";
                target[LocalizationKeys.DiscordHomeReadyChangePagesDescription] = "- Cambia entre paginas, con o sin filtros.\n**Ejemplo:** `{0}hrl 5 Charmander`";
                target[LocalizationKeys.DiscordHomeReadyTradeFileTitle] = "TRADEAR ARCHIVO - `{0}hrr <numero>`";
                target[LocalizationKeys.DiscordHomeReadyTradeFileDescription] = "- Tradea el Pokemon por su numero en la lista.\n**Ejemplo:** `{0}hrr 682`";
                target[LocalizationKeys.DiscordHomeReadyNotConfigured] = "Este bot no tiene configurado el modulo HOME-Ready.";
                target[LocalizationKeys.DiscordHomeReadyAlreadyInQueue] = "Ya estas en una cola de trade. Termina esa primero.";
                target[LocalizationKeys.DiscordHomeReadyNoPkmFiles] = "No se encontraron archivos PKM HOME-ready.";
                target[LocalizationKeys.DiscordHomeReadyNoFiles] = "No se encontraron archivos HOME-ready.";
                target[LocalizationKeys.DiscordHomeReadyInvalidEntry] = "Numero de entrada invalido. Elige un archivo valido.";
                target[LocalizationKeys.DiscordHomeReadyConvertInvalid] = "No se pudo convertir el archivo a un PKM valido.";
                target[LocalizationKeys.DiscordHomeReadyConvertTradeFormatFailed] = "No se pudo convertir el archivo HOME-ready al formato de trade de tu juego.";
                target[LocalizationKeys.DiscordHomeReadyAddedQueue] = "**Pokemon HOME-Ready agregado a la cola.**";
                target[LocalizationKeys.DiscordHomeReadyNoMatches] = "No se encontraron archivos HOME-ready para '{0}'.";
                target[LocalizationKeys.DiscordHomeReadyListTitle] = "Archivos HOME-Ready - '{0}'";
                target[LocalizationKeys.DiscordHomeReadyListDescription] = "Pagina **{0}** de **{1}**";
                target[LocalizationKeys.DiscordHomeReadyListField] = "Usa `{0}hrr {1}` para pedir este Pokemon.\nUsa `{0}hrv {1}` para ver detalles del Pokemon.\nUsa `{0}hrd {1}` para descargar este archivo PKM.\nEste archivo es para **{2}**.";
                target[LocalizationKeys.DiscordHomeReadyReadFailed] = "No se pudo leer el archivo PKM.";
                target[LocalizationKeys.DiscordHomeReadyLoadedConvertFailed] = "El archivo cargo, pero no se pudo convertir a la generacion de tu juego.";
                target[LocalizationKeys.DiscordHomeReadyTrackerEmpty] = "N/A - Solo tradea con un bot de **{0}**.";
                target[LocalizationKeys.DiscordHomeReadyAdditionalDetails] = "**Detalles adicionales**\nâ€¢ OT: {0}\nâ€¢ TID: {1}\nâ€¢ Juego: {2}\nâ€¢ Lugar encontrado: {3} ({4})\nâ€¢ Fecha encontrada: {5}\nâ€¢ Tracker: {6}";
                target[LocalizationKeys.DiscordHomeReadyViewTitle] = "Viendo entrada HOME-Ready #{0}";
                target[LocalizationKeys.DiscordHomeReadyViewFooter] = "Usa {0}hrr {1} para pedir este Pokemon por trade.\nUsa {0}hrd {1} para descargar este archivo PKM.";
                target[LocalizationKeys.DiscordHomeReadyDownloadText] = "Aqui esta tu archivo Pokemon HOME-Ready: **{0}**";
                break;
        }
    }
}
