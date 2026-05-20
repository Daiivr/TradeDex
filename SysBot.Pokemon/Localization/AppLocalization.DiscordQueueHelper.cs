using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordQueueHelperTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: QueueHelper
                target[LocalizationKeys.DiscordTradeCodeRange] = "Trade code should be 00000000-99999999!";
                target[LocalizationKeys.DiscordQueueFullTitle] = "Queue Full";
                target[LocalizationKeys.DiscordQueueFullDescription] = "The queue is currently full ({0}/{0}). Please try again later when space becomes available.";
                target[LocalizationKeys.DiscordQueueFullFooter] = "Queue will open up as trades are completed";
                target[LocalizationKeys.DiscordUserAlreadyInQueue] = "{0} - You are already in the queue!";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedAuthor] = "Error while adding you to the queue";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedErrorField] = "__**Error**__:";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedErrorValue] = "❌ {0} I couldn't add you to the queue.";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedReasonField] = "__**Reason**__:";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedReasonValue] = "You cannot add more operations until your current one is processed.";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedSolutionField] = "__**Solution**__:";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedSolutionValue] = "Wait until the existing operation finishes, then try again.";
                target[LocalizationKeys.DiscordTradeBlockedHeldItemPlza] = "{0} - Trade blocked: the held item '{1}' cannot be traded in PLZA.";
                target[LocalizationKeys.DiscordWaitEstimateTrade] = "Wait Estimate: {0:F1} min(s) for trade.";
                target[LocalizationKeys.DiscordWaitEstimateBatch] = "Wait Estimate: {0:F1} min(s) for batch";
                target[LocalizationKeys.DiscordWaitEstimateTradeNumber] = "Estimated Time: {0:F1} min(s) for trade {1}/{2}.";
                target[LocalizationKeys.DiscordCurrentQueuePosition] = "Current Queue Position: {0}";
                target[LocalizationKeys.DiscordNotice] = "**__Notice__**";
                target[LocalizationKeys.DiscordNonNativeHomeTrackerNotice] = "**This Pokemon is Non-Native & Has Home Tracker.**";
                target[LocalizationKeys.DiscordHomeTrackerNotice] = "**Home Tracker Detected.**";
                target[LocalizationKeys.DiscordNonNativeNotice] = "**This Pokemon is Non-Native.**";
                target[LocalizationKeys.DiscordAutoOtNotApplied] = "*AutoOT not applied.*";
                target[LocalizationKeys.DiscordCannotEnterHomeAutoOt] = "*Cannot enter HOME & AutoOT not applied.*";
                target[LocalizationKeys.DiscordTradeDetailsPrepareError] = "An error occurred while preparing the trade details.";
                target[LocalizationKeys.DiscordHiddenTradeAdded] = "Successfully Added\n//User: ||Owner Access Only||\n//Position: {0}\n//Pokemon: ||{1}||\n//ETA: {2:F1} min(s)";
                target[LocalizationKeys.DiscordBatchAddedSummary] = "{0} - Added batch trade with {1} Pokemon to the queue! Position: {2}. Estimated: {3:F1} min(s).";
                target[LocalizationKeys.DiscordBatchSummaryAuthor] = "Batch Trade - {0}";
                target[LocalizationKeys.DiscordBatchSummaryFooter] = "Batch Trades: {0} Pokemon";
                target[LocalizationKeys.DiscordBatchFooter] = "Batch Trade {0} of {1}";
                target[LocalizationKeys.DiscordBatchAddedPlain] = "{0} - Added batch trade ({1}x) to queue! Position: {2}. Estimated: {3:F1} min(s).";
                target[LocalizationKeys.DiscordMysteryEggBatchDescription] = "You are currently receiving **{0}** Mystery Egg{1}!\nWhat could they be?";
                target[LocalizationKeys.DiscordMysteryEggBatchAuthor] = "{0}'s Mystery Egg Batch Trade";
                target[LocalizationKeys.DiscordMysteryEggBatchAddedPlain] = "{0} - Added Mystery Egg batch ({1}x) to queue! Position: {2}. Estimated: {3:F1} min(s).";
                target[LocalizationKeys.DiscordItemBatchDescription] = "**{0}** will deliver your **{1}** {2}{3}!";
                target[LocalizationKeys.DiscordItemBatchAuthor] = "{0}'s Item Batch Trade";
                target[LocalizationKeys.DiscordItemBatchAddedPlain] = "{0} - Added item batch trade ({1}x) to queue! Position: {2}. Estimated: {3:F1} min(s).";
                break;
            case AppLanguage.Spanish:
                // Discord: QueueHelper
                target[LocalizationKeys.DiscordTradeCodeRange] = "El codigo de trade debe estar entre 00000000 y 99999999!";
                target[LocalizationKeys.DiscordQueueFullTitle] = "Cola llena";
                target[LocalizationKeys.DiscordQueueFullDescription] = "La cola esta llena ({0}/{0}). Intentalo de nuevo mas tarde cuando haya espacio.";
                target[LocalizationKeys.DiscordQueueFullFooter] = "La cola se abrira cuando se completen trades";
                target[LocalizationKeys.DiscordUserAlreadyInQueue] = "{0} - Ya estas en la cola!";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedAuthor] = "Error al intentar agregarte a la lista";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedErrorField] = "__**Error**__:";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedErrorValue] = "❌ {0} No pude agregarte a la cola.";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedReasonField] = "__**Razon**__:";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedReasonValue] = "No puedes agregar mas operaciones hasta que la actual se procese.";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedSolutionField] = "__**Solucion**__:";
                target[LocalizationKeys.DiscordAlreadyInQueueEmbedSolutionValue] = "Espera a que la operacion existente termine e intentalo de nuevo.";
                target[LocalizationKeys.DiscordTradeBlockedHeldItemPlza] = "{0} - Trade bloqueado: el item '{1}' no se puede tradear en PLZA.";
                target[LocalizationKeys.DiscordWaitEstimateTrade] = "Espera estimada: {0:F1} min para el trade.";
                target[LocalizationKeys.DiscordWaitEstimateBatch] = "Espera estimada: {0:F1} min para el lote";
                target[LocalizationKeys.DiscordWaitEstimateTradeNumber] = "Tiempo Estimado: {0:F1} minuto(s) para el trade {1}/{2}.";
                target[LocalizationKeys.DiscordCurrentQueuePosition] = "Posicion actual en cola: {0}";
                target[LocalizationKeys.DiscordNotice] = "**__Aviso__**";
                target[LocalizationKeys.DiscordNonNativeHomeTrackerNotice] = "**Este Pokemon no es nativo y tiene tracker de HOME.**";
                target[LocalizationKeys.DiscordHomeTrackerNotice] = "**Tracker de HOME detectado.**";
                target[LocalizationKeys.DiscordNonNativeNotice] = "**Este Pokemon no es nativo.**";
                target[LocalizationKeys.DiscordAutoOtNotApplied] = "*AutoOT no aplicado.*";
                target[LocalizationKeys.DiscordCannotEnterHomeAutoOt] = "*No puede entrar a HOME y AutoOT no fue aplicado.*";
                target[LocalizationKeys.DiscordTradeDetailsPrepareError] = "Ocurrio un error al preparar los detalles del trade.";
                target[LocalizationKeys.DiscordHiddenTradeAdded] = "Agregado correctamente\n//Usuario: ||Solo acceso de owner||\n//Posicion: {0}\n//Pokemon: ||{1}||\n//ETA: {2:F1} min";
                target[LocalizationKeys.DiscordBatchAddedSummary] = "{0} - Se agrego un lote de {1} Pokemon a la cola! Posicion: {2}. Estimado: {3:F1} min.";
                target[LocalizationKeys.DiscordBatchSummaryAuthor] = "Batch Trade - {0}";
                target[LocalizationKeys.DiscordBatchSummaryFooter] = "Trades por lote: {0} Pokemon";
                target[LocalizationKeys.DiscordBatchFooter] = "Trade por lote {0} de {1}";
                target[LocalizationKeys.DiscordBatchAddedPlain] = "{0} - Se agrego un lote ({1}x) a la cola! Posicion: {2}. Estimado: {3:F1} min.";
                target[LocalizationKeys.DiscordMysteryEggBatchDescription] = "Ahora estas recibiendo **{0}** Huevo{1} Misterioso{1}!\nQue podran ser?";
                target[LocalizationKeys.DiscordMysteryEggBatchAuthor] = "Lote de Huevos Misteriosos de {0}";
                target[LocalizationKeys.DiscordMysteryEggBatchAddedPlain] = "{0} - Se agrego un lote de Huevos Misteriosos ({1}x) a la cola! Posicion: {2}. Estimado: {3:F1} min.";
                target[LocalizationKeys.DiscordItemBatchDescription] = "**{0}** entregara tus **{1}** {2}{3}!";
                target[LocalizationKeys.DiscordItemBatchAuthor] = "Lote de items de {0}";
                target[LocalizationKeys.DiscordItemBatchAddedPlain] = "{0} - Se agrego un lote de items ({1}x) a la cola! Posicion: {2}. Estimado: {3:F1} min.";
                break;
        }
    }
}
