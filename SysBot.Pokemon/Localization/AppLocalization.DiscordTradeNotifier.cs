using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordTradeNotifierTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: DiscordTradeNotifier
                target[LocalizationKeys.DiscordUpNextTitle] = "You're Up Next!";
                target[LocalizationKeys.DiscordUpNextDescription] = "Get ready, your trade will begin shortly.\n\nCurrent queue position: **{0}**.{1}";
                target[LocalizationKeys.DiscordUpNextBatchInfo] = "\n\n**Important:** This is a batch trade with {0} Pokemon. Please stay in the trade until all are completed!";
                target[LocalizationKeys.DiscordUpNextFooter] = "Estimated wait time: {0}";
                target[LocalizationKeys.DiscordQueuePositionUpdateTitle] = "Your Queue Position Updated";
                target[LocalizationKeys.DiscordQueuePositionUpdateDescription] = "You are still in the queue.\n\nCurrent position: **{0}**";
                target[LocalizationKeys.DiscordBatchQueuedTitle] = "Batch Trade Request Queued";
                target[LocalizationKeys.DiscordTradeQueuedTitle] = "Trade Request Queued";
                target[LocalizationKeys.DiscordBatchQueuedDescription] = "Your batch trade request ({0} Pokemon) has been queued.\n\n**Important Instructions:**\n- Stay in the trade for all {0} trades\n- Have all {0} Pokemon ready to trade\n- Do not exit until you see the completion message\n\n**Queue Position**: {1}";
                target[LocalizationKeys.DiscordTradeQueuedDescription] = "Your trade request has been queued.\n**Queue Position**: {0}";
                target[LocalizationKeys.DiscordEstimatedWaitFooter] = "Estimated wait time: {0}";
                target[LocalizationKeys.DiscordLessThanMinute] = "Less than a minute";
                target[LocalizationKeys.DiscordMinutes] = "{0} minutes";
                target[LocalizationKeys.DiscordMysteryEgg] = "Mystery Egg";
                target[LocalizationKeys.DiscordMysteryPokemon] = "Mystery Pokemon";
                target[LocalizationKeys.DiscordBatchTradeStarting] = "Starting your batch trade! Trading {0} Pokemon.\n\n**Trade 1/{0}**: {1}{2}\n\n**IMPORTANT:** Stay in the trade until all {0} trades are completed!";
                target[LocalizationKeys.DiscordBatchTradePreparing] = "Preparing trade {0}/{1}: {2}{3}";
                target[LocalizationKeys.DiscordTradeInitializing] = "Initializing trade{0}. Please be ready.";
                target[LocalizationKeys.DiscordLgTradeInitializing] = "Initializing trade{0}. Please be ready. Your code is";
                target[LocalizationKeys.DiscordWaitingForUser] = "I'm waiting for you{0}{1}! My IGN is **{2}**.";
                target[LocalizationKeys.DiscordBatchSelectFirst] = "Starting batch trade ({0} Pokemon total). **Please select your first Pokemon!**";
                target[LocalizationKeys.DiscordBatchSelectNext] = "Trade {0}/{1}: Now trading {2}. **Select your next Pokemon!**";
                target[LocalizationKeys.DiscordBatchCanceled] = "Batch trade canceled: {0}. All remaining trades have been canceled.";
                target[LocalizationKeys.DiscordTradeResultSuccess] = "Trade completed successfully";
                target[LocalizationKeys.DiscordTradeResultNoTrainerFound] = "No trade partner was found";
                target[LocalizationKeys.DiscordTradeResultTrainerTooSlow] = "The trade partner was too slow";
                target[LocalizationKeys.DiscordTradeResultTrainerLeft] = "The trade partner left the trade";
                target[LocalizationKeys.DiscordTradeResultTrainerOfferCanceledQuick] = "The trade partner canceled the offer too quickly";
                target[LocalizationKeys.DiscordTradeResultTrainerRequestBad] = "The trade partner request was invalid";
                target[LocalizationKeys.DiscordTradeResultIllegalTrade] = "Illegal trade";
                target[LocalizationKeys.DiscordTradeResultSuspiciousActivity] = "Suspicious activity detected";
                target[LocalizationKeys.DiscordTradeResultUserCanceled] = "Trade canceled by the user";
                target[LocalizationKeys.DiscordTradeResultRoutineCancel] = "Routine cancellation by the bot";
                target[LocalizationKeys.DiscordTradeResultExceptionConnection] = "Connection exception";
                target[LocalizationKeys.DiscordTradeResultExceptionInternal] = "Internal system exception";
                target[LocalizationKeys.DiscordTradeResultRecoverStart] = "Recovery started";
                target[LocalizationKeys.DiscordTradeResultRecoverPostLinkCode] = "Recovery after link code";
                target[LocalizationKeys.DiscordTradeResultRecoverOpenBox] = "Recovery while opening the box";
                target[LocalizationKeys.DiscordTradeResultRecoverReturnOverworld] = "Recovery while returning to the overworld";
                target[LocalizationKeys.DiscordTradeResultRecoverEnterUnionRoom] = "Recovery while entering the Union Room";
                target[LocalizationKeys.DiscordTradeResultTradeEvolveNotAllowed] = "This Pokemon cannot evolve by trade";
                target[LocalizationKeys.DiscordBatchAllCompleted] = "**All {0} trades completed successfully!** Thank you for trading!";
                target[LocalizationKeys.DiscordBatchTradeCompleted] = "Trade {0}/{1} completed! ({2})\nPreparing trade {3}/{1}...";
                target[LocalizationKeys.DiscordTradeFinishedEnjoy] = "Trade finished. Enjoy!";
                target[LocalizationKeys.DiscordTradeFinishedEnjoyPokemon] = "Trade finished. Enjoy your **{0}**!";
                target[LocalizationKeys.DiscordTradeFinishedMysteryPokemon] = "Trade finished. You received a **Mystery Pokemon**!";
                target[LocalizationKeys.DiscordTradeFinishedMysteryEgg] = "Trade finished. Enjoy your **Mystery Egg**!";
                target[LocalizationKeys.DiscordTradeFinished] = "Trade finished!";
                target[LocalizationKeys.DiscordReturnPokemon] = "Here's what you traded me!";
                target[LocalizationKeys.DiscordSeedDetails] = "Here are the details for `{0:X16}`:";
                target[LocalizationKeys.DiscordSeedFieldName] = "Seed: {0:X16}";
                break;
            case AppLanguage.Spanish:
                // Discord: DiscordTradeNotifier
                target[LocalizationKeys.DiscordUpNextTitle] = "Tu turno esta por llegar!";
                target[LocalizationKeys.DiscordUpNextDescription] = "Preparate, tu trade comenzara en breve.\n\nPosicion actual en la cola: **{0}**.{1}";
                target[LocalizationKeys.DiscordUpNextBatchInfo] = "\n\n**Importante:** Este es un trade por lote con {0} Pokemon. Mantente en el trade hasta completar todos!";
                target[LocalizationKeys.DiscordUpNextFooter] = "Tiempo estimado de espera: {0}";
                target[LocalizationKeys.DiscordQueuePositionUpdateTitle] = "Actualizacion de tu posicion";
                target[LocalizationKeys.DiscordQueuePositionUpdateDescription] = "Aun estas en la cola.\n\nPosicion actual: **{0}**";
                target[LocalizationKeys.DiscordBatchQueuedTitle] = "Solicitud de lote agregada a la cola";
                target[LocalizationKeys.DiscordTradeQueuedTitle] = "Solicitud de trade agregada a la cola";
                target[LocalizationKeys.DiscordBatchQueuedDescription] = "Tu solicitud de lote ({0} Pokemon) fue agregada a la cola.\n\n**Instrucciones importantes:**\n- Mantente en el trade durante los {0} trades\n- Ten listos los {0} Pokemon para tradear\n- No salgas hasta ver el mensaje de finalizacion\n\n**Posicion en cola**: {1}";
                target[LocalizationKeys.DiscordTradeQueuedDescription] = "Tu solicitud de trade fue agregada a la cola.\n**Posicion en cola**: {0}";
                target[LocalizationKeys.DiscordEstimatedWaitFooter] = "Espera estimada: {0}";
                target[LocalizationKeys.DiscordLessThanMinute] = "Menos de un minuto";
                target[LocalizationKeys.DiscordMinutes] = "{0} minutos";
                target[LocalizationKeys.DiscordMysteryEgg] = "Huevo misterioso";
                target[LocalizationKeys.DiscordMysteryPokemon] = "Pokemon misterioso";
                target[LocalizationKeys.DiscordBatchTradeStarting] = "Iniciando tu lote! Se tradearan {0} Pokemon.\n\n**Trade 1/{0}**: {1}{2}\n\n**IMPORTANTE:** Mantente en el trade hasta completar los {0} trades!";
                target[LocalizationKeys.DiscordBatchTradePreparing] = "Preparando trade {0}/{1}: {2}{3}";
                target[LocalizationKeys.DiscordTradeInitializing] = "Inicializando trade{0}. Ten todo listo.";
                target[LocalizationKeys.DiscordLgTradeInitializing] = "Inicializando trade{0}. Ten todo listo. Tu codigo es";
                target[LocalizationKeys.DiscordWaitingForUser] = "Te estoy esperando{0}{1}! Mi IGN es **{2}**.";
                target[LocalizationKeys.DiscordBatchSelectFirst] = "Iniciando lote ({0} Pokemon en total). **Selecciona tu primer Pokemon!**";
                target[LocalizationKeys.DiscordBatchSelectNext] = "Trade {0}/{1}: Ahora tradeando {2}. **Selecciona tu siguiente Pokemon!**";
                target[LocalizationKeys.DiscordBatchCanceled] = "Lote cancelado: {0}. Todos los trades restantes fueron cancelados.";
                target[LocalizationKeys.DiscordTradeResultSuccess] = "Intercambio exitoso";
                target[LocalizationKeys.DiscordTradeResultNoTrainerFound] = "No se encontro compañero de intercambio";
                target[LocalizationKeys.DiscordTradeResultTrainerTooSlow] = "El compañero de intercambio fue demasiado lento";
                target[LocalizationKeys.DiscordTradeResultTrainerLeft] = "El compañero de intercambio abandono el intercambio";
                target[LocalizationKeys.DiscordTradeResultTrainerOfferCanceledQuick] = "El compañero de intercambio cancelo la oferta demasiado rapido";
                target[LocalizationKeys.DiscordTradeResultTrainerRequestBad] = "Solicitud del compañero de intercambio invalida";
                target[LocalizationKeys.DiscordTradeResultIllegalTrade] = "Intercambio ilegal";
                target[LocalizationKeys.DiscordTradeResultSuspiciousActivity] = "Actividad sospechosa detectada";
                target[LocalizationKeys.DiscordTradeResultUserCanceled] = "Intercambio cancelado por el usuario";
                target[LocalizationKeys.DiscordTradeResultRoutineCancel] = "Cancelacion de rutina por el bot";
                target[LocalizationKeys.DiscordTradeResultExceptionConnection] = "Excepcion de conexion";
                target[LocalizationKeys.DiscordTradeResultExceptionInternal] = "Excepcion interna del sistema";
                target[LocalizationKeys.DiscordTradeResultRecoverStart] = "Recuperacion iniciada";
                target[LocalizationKeys.DiscordTradeResultRecoverPostLinkCode] = "Recuperacion despues de codigo de enlace";
                target[LocalizationKeys.DiscordTradeResultRecoverOpenBox] = "Recuperacion al abrir caja";
                target[LocalizationKeys.DiscordTradeResultRecoverReturnOverworld] = "Recuperacion al volver al mundo";
                target[LocalizationKeys.DiscordTradeResultRecoverEnterUnionRoom] = "Recuperacion al entrar en la sala de union";
                target[LocalizationKeys.DiscordTradeResultTradeEvolveNotAllowed] = "No se permite evolucionar este Pokemon por intercambio";
                target[LocalizationKeys.DiscordBatchAllCompleted] = "**Los {0} trades se completaron correctamente!** Gracias por tradear!";
                target[LocalizationKeys.DiscordBatchTradeCompleted] = "Trade {0}/{1} completado! ({2})\nPreparando trade {3}/{1}...";
                target[LocalizationKeys.DiscordTradeFinishedEnjoy] = "Trade terminado. Disfrutalo!";
                target[LocalizationKeys.DiscordTradeFinishedEnjoyPokemon] = "Trade terminado. Disfruta de tu **{0}**!";
                target[LocalizationKeys.DiscordTradeFinishedMysteryPokemon] = "Trade terminado. Recibiste un **Pokemon misterioso**!";
                target[LocalizationKeys.DiscordTradeFinishedMysteryEgg] = "Trade terminado. Disfruta de tu **Huevo misterioso**!";
                target[LocalizationKeys.DiscordTradeFinished] = "Trade terminado!";
                target[LocalizationKeys.DiscordReturnPokemon] = "Esto es lo que me tradeaste!";
                target[LocalizationKeys.DiscordSeedDetails] = "Aqui estan los detalles para `{0:X16}`:";
                target[LocalizationKeys.DiscordSeedFieldName] = "Seed: {0:X16}";
                break;
        }
    }
}
