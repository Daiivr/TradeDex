using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddLogsMysteryMonModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Logs: MysteryMonModule
                target[LocalizationKeys.LogSlashCommandsRegistered] = "Slash commands registered globally!";
                target[LocalizationKeys.LogSlashCommandsRegisterFailed] = "Failed to register slash commands: {0}";
                target[LocalizationKeys.LogInteractionHandleError] = "Error handling interaction: {0}";
                target[LocalizationKeys.LogMissingMessagePermission] = "Missing permissions to handle a message in channel {0}";
                target[LocalizationKeys.LogUnhandledHandleMessage] = "Unhandled exception in HandleMessageAsync: {0}";
                target[LocalizationKeys.LogGatewayHandlerBlocking] = "A MessageReceived handler is blocking the gateway task. Method: HandleMessageAsync, Execution Time: {0}ms, Message Content: {1}...";
                target[LocalizationKeys.LogExecutingCommand] = "Executing command from {0}#{1}:@{2}. Content: {3}";
                target[LocalizationKeys.LogErrorExecutingCommand] = "Error executing command: {0}";
                target[LocalizationKeys.LogMissingSendPermission] = "Missing permissions to send message in channel {0}";
                target[LocalizationKeys.LogErrorSendingMessage] = "Error sending message: {0}";
                target[LocalizationKeys.LogLoggingEchoLoaded] = "Logging and Echo channels loaded!";
                target[LocalizationKeys.LogBotConfigReadError] = "Error reading config file: {0}";
                target[LocalizationKeys.LogSmogonScrapeError] = "An error occurred while scraping the Smogon set: {0}";
                target[LocalizationKeys.LogSmogonHttpError] = "HttpRequestException occurred while scraping: {0}";
                target[LocalizationKeys.LogSmogonGenericError] = "Exception occurred while scraping: {0}";
                target[LocalizationKeys.LogEchoQueueStatusFailed] = "Failed to send queue status to channel {0}: {1}";
                target[LocalizationKeys.LogEchoChannelAccessFailed] = "Failed to find or access channel {0}";
                target[LocalizationKeys.LogEchoAnnouncementFailed] = "Failed to send announcement to channel {0}: {1}";
                target[LocalizationKeys.LogEchoMessageFailed] = "Failed to send message to channel '{0}' (Attempt {1}): {2}";
                target[LocalizationKeys.LogEchoEmbedFailed] = "Failed to send embed to channel '{0}' (Attempt {1}): {2}";
                target[LocalizationKeys.LogRecoveryNotificationFailed] = "Failed to send recovery notification to Discord: {0}";
                target[LocalizationKeys.LogTradeStartSkipped] = "Trade start notification skipped (Discord {0}): {1}";
                target[LocalizationKeys.LogTradeStartFailed] = "Trade start notification failed: {0}";
                target[LocalizationKeys.LogMysteryMonBatchError] = "Batch MysteryMon error: {0}";
                target[LocalizationKeys.LogMysteryMonLegalityRetry] = "[MysteryMon] Species {0} failed final legality check, retrying outer loop";
                target[LocalizationKeys.LogMysteryMonAllSpeciesFailed] = "[MysteryMon] All species attempts failed, returning null";
                target[LocalizationKeys.LogMysteryMonStepOk] = "[MysteryMon] Step '{0}' -> OK on attempt {1}";
                target[LocalizationKeys.LogMysteryMonStepFailed] = "[MysteryMon] Step '{0}' -> FAILED after {1} attempts";
                target[LocalizationKeys.LogMysteryMonNoPersonalTable] = "[MysteryMon] No personal table available for {0}";
                break;
            case AppLanguage.Spanish:
                // Logs: MysteryMonModule
                target[LocalizationKeys.LogSlashCommandsRegistered] = "Comandos slash registrados globalmente!";
                target[LocalizationKeys.LogSlashCommandsRegisterFailed] = "No se pudieron registrar los comandos slash: {0}";
                target[LocalizationKeys.LogInteractionHandleError] = "Error manejando interaccion: {0}";
                target[LocalizationKeys.LogMissingMessagePermission] = "Faltan permisos para manejar un mensaje en el canal {0}";
                target[LocalizationKeys.LogUnhandledHandleMessage] = "Excepcion no controlada en HandleMessageAsync: {0}";
                target[LocalizationKeys.LogGatewayHandlerBlocking] = "Un handler MessageReceived esta bloqueando la tarea del gateway. Metodo: HandleMessageAsync, Tiempo de ejecucion: {0}ms, Contenido del mensaje: {1}...";
                target[LocalizationKeys.LogExecutingCommand] = "Ejecutando comando desde {0}#{1}:@{2}. Contenido: {3}";
                target[LocalizationKeys.LogErrorExecutingCommand] = "Error ejecutando comando: {0}";
                target[LocalizationKeys.LogMissingSendPermission] = "Faltan permisos para enviar mensaje en el canal {0}";
                target[LocalizationKeys.LogErrorSendingMessage] = "Error enviando mensaje: {0}";
                target[LocalizationKeys.LogLoggingEchoLoaded] = "Canales de logs y Echo cargados!";
                target[LocalizationKeys.LogBotConfigReadError] = "Error leyendo el archivo de configuracion: {0}";
                target[LocalizationKeys.LogSmogonScrapeError] = "Ocurrio un error al extraer el set de Smogon: {0}";
                target[LocalizationKeys.LogSmogonHttpError] = "HttpRequestException al extraer datos: {0}";
                target[LocalizationKeys.LogSmogonGenericError] = "Excepcion al extraer datos: {0}";
                target[LocalizationKeys.LogEchoQueueStatusFailed] = "No se pudo enviar el estado de cola al canal {0}: {1}";
                target[LocalizationKeys.LogEchoChannelAccessFailed] = "No se pudo encontrar o acceder al canal {0}";
                target[LocalizationKeys.LogEchoAnnouncementFailed] = "No se pudo enviar el anuncio al canal {0}: {1}";
                target[LocalizationKeys.LogEchoMessageFailed] = "No se pudo enviar el mensaje al canal '{0}' (Intento {1}): {2}";
                target[LocalizationKeys.LogEchoEmbedFailed] = "No se pudo enviar el embed al canal '{0}' (Intento {1}): {2}";
                target[LocalizationKeys.LogRecoveryNotificationFailed] = "No se pudo enviar la notificacion de recuperacion a Discord: {0}";
                target[LocalizationKeys.LogTradeStartSkipped] = "Notificacion de inicio de trade omitida (Discord {0}): {1}";
                target[LocalizationKeys.LogTradeStartFailed] = "Fallo la notificacion de inicio de trade: {0}";
                target[LocalizationKeys.LogMysteryMonBatchError] = "Error en lote MysteryMon: {0}";
                target[LocalizationKeys.LogMysteryMonLegalityRetry] = "[MysteryMon] La especie {0} fallo la revision final de legalidad; reintentando ciclo externo";
                target[LocalizationKeys.LogMysteryMonAllSpeciesFailed] = "[MysteryMon] Fallaron todos los intentos de especies; devolviendo null";
                target[LocalizationKeys.LogMysteryMonStepOk] = "[MysteryMon] Paso '{0}' -> OK en el intento {1}";
                target[LocalizationKeys.LogMysteryMonStepFailed] = "[MysteryMon] Paso '{0}' -> FALLO despues de {1} intentos";
                target[LocalizationKeys.LogMysteryMonNoPersonalTable] = "[MysteryMon] No hay tabla personal disponible para {0}";
                break;
        }
    }
}
