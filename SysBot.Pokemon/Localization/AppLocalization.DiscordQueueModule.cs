using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordQueueModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: QueueModule
                target[LocalizationKeys.DiscordQueueModeChanged] = "Changed queue mode to {0}.";
                target[LocalizationKeys.DiscordQueueClearedAll] = "Cleared all in the queue.";
                target[LocalizationKeys.DiscordNoUsersMentioned] = "No users mentioned";
                target[LocalizationKeys.DiscordNotCurrentlyInQueue] = "You are not currently in the queue.";
                target[LocalizationKeys.DiscordQueueListEmpty] = "Queue list is empty.";
                target[LocalizationKeys.DiscordCurrentTradeQueueTitle] = "Current Trade Queue ({0} users)";
                target[LocalizationKeys.DiscordQueueListTruncated] = "... (list truncated)";
                target[LocalizationKeys.DiscordQueueEnabled] = "{0} Queue settings changed: users can now join the trade queue.";
                target[LocalizationKeys.DiscordQueueDisabled] = "{0} Queue settings changed: **Users CANNOT join the queue until it is turned back on.**";
                target[LocalizationKeys.DiscordTradeCodeDeleted] = "{0}, your stored trade code has been deleted successfully.";
                target[LocalizationKeys.DiscordTradeCodeNotFound] = "{0}, no stored trade code was found for your user ID.";
                target[LocalizationKeys.DiscordClearTradeRemoved] = "{0}, I removed your pending trades from the queue.";
                target[LocalizationKeys.DiscordClearTradeProcessing] = "{0}, it looks like you have trades currently being processed! I did not remove those from the queue.";
                target[LocalizationKeys.DiscordClearTradeProcessingRemoved] = "{0}, it looks like you have trades currently being processed. I removed other pending trades from the queue.";
                target[LocalizationKeys.DiscordClearTradeNotInQueue] = "Sorry {0}, you are not currently in the queue.";
                target[LocalizationKeys.DiscordTradeCodeUpdated] = "{0}, your trade code has been successfully updated.";
                target[LocalizationKeys.DiscordTradeCodeMissing] = "{0}, you don't have a trade code set. Use the trade command to generate one first.";
                target[LocalizationKeys.DiscordTradeCodeChangeError] = "{0}, an error occurred while changing your trade code. Please try again later.";
                target[LocalizationKeys.DiscordTradeCodeLength] = "{0}, trade code must be exactly 8 digits long.";
                target[LocalizationKeys.DiscordTradeCodeDigits] = "{0}, trade code must contain only digits.";
                target[LocalizationKeys.DiscordTradeCodeTooEasy] = "{0}, trade code is too easy to guess. Please choose a more complex code.";
                target[LocalizationKeys.DiscordQueueListEmptyTitle] = "Queue List";
                target[LocalizationKeys.DiscordQueueListEmptyDescription] = "The queue list is currently empty.";
                target[LocalizationKeys.DiscordQueueListEmptyDmSent] = "📩 {0}, I sent you the queue list by DM, but it is empty.";
                target[LocalizationKeys.DiscordQueueListDmFailed] = "❌ {0}, I could not send you a DM. Enable your direct messages or send me a DM first.";
                target[LocalizationKeys.DiscordQueueListPageTitle] = "Queued Users - Page {0}/{1}";
                target[LocalizationKeys.DiscordQueueListTotalFooter] = "Total in queue: {0}";
                target[LocalizationKeys.DiscordQueueListDmSentTotal] = "📩 {0}, the queue list (total: **{1}**) was sent to your direct messages.";
                target[LocalizationKeys.DiscordTradeCodeRangeMention] = "{0}, sorry, trade code must be between **00000000** and **99999999**.";
                target[LocalizationKeys.DiscordTradeCodeStoredTitle] = "Trade Code Stored";
                target[LocalizationKeys.DiscordTradeCodeStoredDescription] = "{0}, your trade code has been stored successfully.\n\n__**Code:**__\n# {1}";
                target[LocalizationKeys.DiscordTradeCodeExistingTitle] = "Existing Trade Code";
                target[LocalizationKeys.DiscordTradeCodeExistingDescription] = "{0}, you already have a trade code set.";
                target[LocalizationKeys.DiscordTradeCodeExistingField] = "__**Existing Code**__";
                target[LocalizationKeys.DiscordTradeCodeExistingValue] = "Your current code is:\n __**{0}**__";
                target[LocalizationKeys.DiscordTradeCodeSolutionField] = "__**Solution**__";
                target[LocalizationKeys.DiscordTradeCodeReasonField] = "__**Reason**__";
                target[LocalizationKeys.DiscordTradeCodeAddSolution] = "If you want to change it, use `{0}utc` followed by the new code.";
                target[LocalizationKeys.DiscordTradeCodeUpdateTitle] = "Trade Code Updated";
                target[LocalizationKeys.DiscordTradeCodeUpdateDescription] = "{0}, your trade code has been updated successfully.\n\n__**New Code:**__\n# **{1}**";
                target[LocalizationKeys.DiscordTradeCodeUpdateErrorTitle] = "Trade Code Update Error";
                target[LocalizationKeys.DiscordTradeCodeUpdateErrorDescription] = "{0}, there was a problem updating your trade code.";
                target[LocalizationKeys.DiscordTradeCodeMissingReason] = "It looks like you have not set a permanent trade code yet.";
                target[LocalizationKeys.DiscordTradeCodeUpdateSolution] = "If you want to set a code, use `{0}atc` followed by the code.";
                target[LocalizationKeys.DiscordTradeCodeDeleteTitle] = "Trade Code Deleted";
                target[LocalizationKeys.DiscordTradeCodeDeleteDescription] = "{0}, your trade code has been deleted successfully.";
                target[LocalizationKeys.DiscordTradeCodeDeleteErrorTitle] = "Trade Code Delete Error";
                target[LocalizationKeys.DiscordTradeCodeDeleteErrorDescription] = "{0}, your trade code could not be deleted.";
                target[LocalizationKeys.DiscordTradeCodeDeleteMissingReason] = "You may not have a trade code set.";
                target[LocalizationKeys.DiscordTradeCodeDeleteSolution] = "To set a code, use `{0}atc` followed by the code you want.";
                break;
            case AppLanguage.Spanish:
                // Discord: QueueModule
                target[LocalizationKeys.DiscordQueueModeChanged] = "Modo de cola cambiado a {0}.";
                target[LocalizationKeys.DiscordQueueClearedAll] = "Se limpio toda la cola.";
                target[LocalizationKeys.DiscordNoUsersMentioned] = "No se mencionaron usuarios";
                target[LocalizationKeys.DiscordNotCurrentlyInQueue] = "No estas actualmente en la cola.";
                target[LocalizationKeys.DiscordQueueListEmpty] = "La lista de cola esta vacia.";
                target[LocalizationKeys.DiscordCurrentTradeQueueTitle] = "Cola de trade actual ({0} usuarios)";
                target[LocalizationKeys.DiscordQueueListTruncated] = "... (lista truncada)";
                target[LocalizationKeys.DiscordQueueEnabled] = "{0} Configuracion de cola modificada: los usuarios ahora pueden unirse a la cola de trade.";
                target[LocalizationKeys.DiscordQueueDisabled] = "{0} Configuracion de cola modificada: **Los usuarios NO pueden unirse a la cola hasta que se reactive.**";
                target[LocalizationKeys.DiscordTradeCodeDeleted] = "{0}, tu codigo de trade guardado se elimino correctamente.";
                target[LocalizationKeys.DiscordTradeCodeNotFound] = "{0}, no se encontro un codigo de trade guardado para tu ID de usuario.";
                target[LocalizationKeys.DiscordClearTradeRemoved] = "{0}, elimine tus trades pendientes de la cola.";
                target[LocalizationKeys.DiscordClearTradeProcessing] = "{0}, parece que actualmente tienes trades en proceso! No los elimine de la cola.";
                target[LocalizationKeys.DiscordClearTradeProcessingRemoved] = "{0}, parece que tienes trades en proceso. Se eliminaron otros trades pendientes de la cola.";
                target[LocalizationKeys.DiscordClearTradeNotInQueue] = "Lo siento {0}, actualmente no estas en la cola.";
                target[LocalizationKeys.DiscordTradeCodeUpdated] = "{0}, tu codigo de trade se actualizo correctamente.";
                target[LocalizationKeys.DiscordTradeCodeMissing] = "{0}, no tienes un codigo de trade configurado. Usa el comando de trade para generar uno primero.";
                target[LocalizationKeys.DiscordTradeCodeChangeError] = "{0}, ocurrio un error al cambiar tu codigo de trade. Intentalo de nuevo mas tarde.";
                target[LocalizationKeys.DiscordTradeCodeLength] = "{0}, el codigo de trade debe tener exactamente 8 digitos.";
                target[LocalizationKeys.DiscordTradeCodeDigits] = "{0}, el codigo de trade solo debe contener digitos.";
                target[LocalizationKeys.DiscordTradeCodeTooEasy] = "{0}, el codigo de trade es demasiado facil de adivinar. Elige uno mas complejo.";
                target[LocalizationKeys.DiscordQueueListEmptyTitle] = "Lista de espera";
                target[LocalizationKeys.DiscordQueueListEmptyDescription] = "La lista de espera esta vacia actualmente.";
                target[LocalizationKeys.DiscordQueueListEmptyDmSent] = "📩 {0}, te envie por DM la lista de espera, pero esta vacia.";
                target[LocalizationKeys.DiscordQueueListDmFailed] = "❌ {0}, no pude enviarte MD. Activa tus mensajes directos o enviame un DM primero.";
                target[LocalizationKeys.DiscordQueueListPageTitle] = "Usuarios en cola - Pagina {0}/{1}";
                target[LocalizationKeys.DiscordQueueListTotalFooter] = "Total en cola: {0}";
                target[LocalizationKeys.DiscordQueueListDmSentTotal] = "📩 {0}, la lista de espera (total: **{1}**) fue enviada a tus mensajes directos.";
                target[LocalizationKeys.DiscordTradeCodeRangeMention] = "{0}, lo siento, el codigo de trade debe estar entre **00000000** y **99999999**.";
                target[LocalizationKeys.DiscordTradeCodeStoredTitle] = "Codigo de trade almacenado";
                target[LocalizationKeys.DiscordTradeCodeStoredDescription] = "{0}, tu codigo de trade ha sido almacenado correctamente.\n\n__**Codigo:**__\n# {1}";
                target[LocalizationKeys.DiscordTradeCodeExistingTitle] = "Codigo de trade existente";
                target[LocalizationKeys.DiscordTradeCodeExistingDescription] = "{0}, ya tienes un codigo de trade establecido.";
                target[LocalizationKeys.DiscordTradeCodeExistingField] = "__**Codigo existente**__";
                target[LocalizationKeys.DiscordTradeCodeExistingValue] = "Tu codigo actual es:\n __**{0}**__";
                target[LocalizationKeys.DiscordTradeCodeSolutionField] = "__**Solucion**__";
                target[LocalizationKeys.DiscordTradeCodeReasonField] = "__**Razon**__";
                target[LocalizationKeys.DiscordTradeCodeAddSolution] = "Si deseas cambiarlo, usa `{0}utc` seguido del nuevo codigo.";
                target[LocalizationKeys.DiscordTradeCodeUpdateTitle] = "Codigo de trade actualizado";
                target[LocalizationKeys.DiscordTradeCodeUpdateDescription] = "{0}, tu codigo de trade se ha actualizado correctamente.\n\n__**Nuevo codigo:**__\n# **{1}**";
                target[LocalizationKeys.DiscordTradeCodeUpdateErrorTitle] = "Error al actualizar codigo de trade";
                target[LocalizationKeys.DiscordTradeCodeUpdateErrorDescription] = "{0}, hubo un problema al actualizar tu codigo de trade.";
                target[LocalizationKeys.DiscordTradeCodeMissingReason] = "Al parecer, aun no has establecido un codigo de trade permanente.";
                target[LocalizationKeys.DiscordTradeCodeUpdateSolution] = "Si deseas establecer un codigo, usa `{0}atc` seguido del codigo.";
                target[LocalizationKeys.DiscordTradeCodeDeleteTitle] = "Codigo de trade eliminado";
                target[LocalizationKeys.DiscordTradeCodeDeleteDescription] = "{0}, tu codigo de trade se ha eliminado correctamente.";
                target[LocalizationKeys.DiscordTradeCodeDeleteErrorTitle] = "Error al eliminar codigo de trade";
                target[LocalizationKeys.DiscordTradeCodeDeleteErrorDescription] = "{0}, no se pudo eliminar tu codigo de trade.";
                target[LocalizationKeys.DiscordTradeCodeDeleteMissingReason] = "Es posible que no tengas un codigo de trade establecido.";
                target[LocalizationKeys.DiscordTradeCodeDeleteSolution] = "Para establecer un codigo, usa `{0}atc` seguido del codigo que deseas.";
                break;
        }
    }
}
