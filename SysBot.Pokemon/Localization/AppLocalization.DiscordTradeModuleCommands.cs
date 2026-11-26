using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordTradeModuleCommandsTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: TradeModule commands
                target[LocalizationKeys.DiscordTooManyMentions] = "Too many mentions. Queue one user at a time.";
                target[LocalizationKeys.DiscordUserMentionRequired] = "A user must be mentioned in order to do this.";
                target[LocalizationKeys.DiscordMedalsNoTrades] = "{0}, you haven't made any trades yet.\nStart trading to earn your first medal!";
                target[LocalizationKeys.DiscordTextTradeUnsupportedFile] = "Only `.txt`, `.csv`, `.rtf`, `.docx`, and `.pdf` files are supported for TextTrade.";
                target[LocalizationKeys.DiscordTextTradeNoValidSets] = "No valid Pokemon sets found in the uploaded file.";
                target[LocalizationKeys.DiscordTextTradeDetectedTitle] = "Text Trade Detected!";
                target[LocalizationKeys.DiscordTextTradeDetectedDescription] = "Detected **{0}** Pokemon sets from **{1}**";
                target[LocalizationKeys.DiscordTextTradeFieldInstructions] = "Use `{0}tt {1}` to trade this Pokemon\nUse `{0}tv {1}` to view this Pokemon set";
                target[LocalizationKeys.DiscordTextTradeHowToTradeTitle] = "How to Trade";
                target[LocalizationKeys.DiscordTextTradeHowToTradeValue] = "â€¢ Single trade: `{0}tt 1`\nâ€¢ Multiple trades (batch): `{0}tt 1 2 3`\nâ€¢ Max: **{1} Pokemon** per batch";
                target[LocalizationKeys.DiscordTextTradeFooter] = "Shiny | Fishy | No Held Item | Has OT/TID/SID | Egg\nMake a selection within 60s or the TextTrade is canceled automatically.";
                target[LocalizationKeys.DiscordTextTradeExpired] = "⌛ {0}, your TextTrade request expired after 80 seconds.";
                target[LocalizationKeys.DiscordTextTradeNoFile] = "⚠️ {0}, you haven't uploaded a file yet or it expired. Attach a text-based file first.";
                target[LocalizationKeys.DiscordTextTradeInvalidSelection] = "Invalid selection. Use `{0}tt 1` for single trade or `{0}tt 1 2 3` for batch.";
                target[LocalizationKeys.DiscordTextTradeSelectionLimit] = "You can only trade up to {0} Pokemon at a time. You selected {1}.";
                target[LocalizationKeys.DiscordTextTradeProcessing] = "{0} Processing your text trade with {1} Pokemon...";
                target[LocalizationKeys.DiscordTextTradeProcessingError] = "{0} An error occurred while processing your text trade. Please try again.";
                target[LocalizationKeys.DiscordTextTradeNoActiveFile] = "{0}, you don't have an active TextTrade file loaded. Upload one first with `{1}tt`.";
                target[LocalizationKeys.DiscordTextTradeInvalidSetNumber] = "Invalid set number. Use `{0}tv 1` through `{0}tv {1}`.";
                target[LocalizationKeys.DiscordTextTradeViewTitle] = "Viewing Set #{0}";
                target[LocalizationKeys.DiscordTextTradeViewFooter] = "Use {0}tt {1} to trade this Pokemon.";
                target[LocalizationKeys.DiscordBatchTradeLimit] = "You can only process up to {0} trades at a time.\nPlease reduce the number of trades in your batch.";
                target[LocalizationKeys.DiscordBatchTradeProcessing] = "{0} Processing your batch trade with {1} Pokemon...";
                target[LocalizationKeys.DiscordBatchTradeProcessingError] = "{0} An error occurred while processing your batch trade. Please try again.";
                target[LocalizationKeys.DiscordBatchZipMissingAttachment] = "{0} Attach a `.zip`, `.rar`, or `.7z` containing PKM files.";
                target[LocalizationKeys.DiscordBatchZipInvalidArchive] = "{0} Only **.zip**, **.rar**, and **.7z** archives are accepted.";
                target[LocalizationKeys.DiscordBatchZipDownloadFailed] = "{0} Failed to download the file: {1}";
                target[LocalizationKeys.DiscordBatchZipProcessing] = "{0} Processing your archive & extracting files...";
                target[LocalizationKeys.DiscordBatchZipLimit] = "{0} You included {1} Pokemon but the limit is {2}.";
                target[LocalizationKeys.DiscordBatchZipNoValidPokemon] = "{0} Your archive contained no valid Pokemon.";
                target[LocalizationKeys.DiscordBatchZipUnexpectedError] = "{0} An unexpected error occurred. Check logs.";
                target[LocalizationKeys.DiscordEggTimeout] = "Egg generation took too long and the bot timed out.";
                target[LocalizationKeys.DiscordEggFailed] = "Failed to generate egg from the provided set.\nTry to remove possible illegal lines and try again.";
                target[LocalizationKeys.DiscordEggCreateFailed] = "Oops! I wasn't able to create an egg for that.\nTry to remove possible illegal lines and try again";
                target[LocalizationKeys.DiscordRequestProcessingError] = "An error occurred while processing the request.";
                target[LocalizationKeys.DiscordLanguageNotRecognized] = "Couldn't recognize language: {0}.";
                target[LocalizationKeys.DiscordLegalizeTimeout] = "Set took too long to legalize.";
                target[LocalizationKeys.DiscordGenerateTimeout] = "That set took too long to generate.";
                target[LocalizationKeys.DiscordCreateSomethingFailed] = "I wasn't able to create something from that.";
                target[LocalizationKeys.DiscordBestAttemptDitto] = "⚠️ {1}, oops! {0} Here's my best attempt for that Ditto!";
                target[LocalizationKeys.DiscordDittoInvalidIVSpread] = "Invalid IV spread: `{0}`. Each value must be between 0 and 31.";
                target[LocalizationKeys.DiscordDittoUnknownArguments] = "{1}, unrecognized Ditto argument(s): `{0}`. Use IVs (for example, 31/31/31/31/31/31), language, nature, Shiny, OT/TID/SID or origin game.";
                target[LocalizationKeys.DiscordDittoAdAttempt] = "{0} tried to generate a Pokemon containing advertising on {1}.\nEveryone laugh at them and call them stupid.";
                target[LocalizationKeys.DiscordItemNotRecognized] = "{0}, the item you entered wasn't recognized.";
                target[LocalizationKeys.DiscordBestAttemptSpecies] = "{0}\nHere's my best attempt for that {1}!";
                break;
            case AppLanguage.Spanish:
                // Discord: TradeModule commands
                target[LocalizationKeys.DiscordTooManyMentions] = "Demasiadas menciones. Pon un usuario en cola a la vez.";
                target[LocalizationKeys.DiscordUserMentionRequired] = "Debes mencionar a un usuario para hacer esto.";
                target[LocalizationKeys.DiscordMedalsNoTrades] = "{0}, todavia no has hecho ningun trade.\nEmpieza a tradear para ganar tu primera medalla!";
                target[LocalizationKeys.DiscordTextTradeUnsupportedFile] = "TextTrade solo soporta archivos `.txt`, `.csv`, `.rtf`, `.docx` y `.pdf`.";
                target[LocalizationKeys.DiscordTextTradeNoValidSets] = "No se encontraron sets Pokemon validos en el archivo subido.";
                target[LocalizationKeys.DiscordTextTradeDetectedTitle] = "Text Trade detectado!";
                target[LocalizationKeys.DiscordTextTradeDetectedDescription] = "Se detectaron **{0}** sets Pokemon en **{1}**";
                target[LocalizationKeys.DiscordTextTradeFieldInstructions] = "Usa `{0}tt {1}` para tradear este Pokemon\nUsa `{0}tv {1}` para ver este set Pokemon";
                target[LocalizationKeys.DiscordTextTradeHowToTradeTitle] = "Como tradear";
                target[LocalizationKeys.DiscordTextTradeHowToTradeValue] = "â€¢ Trade individual: `{0}tt 1`\nâ€¢ Varios trades (lote): `{0}tt 1 2 3`\nâ€¢ Maximo: **{1} Pokemon** por lote";
                target[LocalizationKeys.DiscordTextTradeFooter] = "Shiny | Sospechoso | Sin item | Tiene OT/TID/SID | Huevo\nHaz una seleccion en 60s o TextTrade se cancelara automaticamente.";
                target[LocalizationKeys.DiscordTextTradeExpired] = "⌛ {0}, tu solicitud TextTrade expiro despues de 80 segundos.";
                target[LocalizationKeys.DiscordTextTradeNoFile] = "⚠️ {0}, todavia no has subido un archivo o ya expiro. Adjunta primero un archivo de texto.";
                target[LocalizationKeys.DiscordTextTradeInvalidSelection] = "Seleccion invalida. Usa `{0}tt 1` para trade individual o `{0}tt 1 2 3` para lote.";
                target[LocalizationKeys.DiscordTextTradeSelectionLimit] = "Solo puedes tradear hasta {0} Pokemon a la vez. Seleccionaste {1}.";
                target[LocalizationKeys.DiscordTextTradeProcessing] = "{0} Procesando tu text trade con {1} Pokemon...";
                target[LocalizationKeys.DiscordTextTradeProcessingError] = "{0} Ocurrio un error al procesar tu text trade. Intentalo de nuevo.";
                target[LocalizationKeys.DiscordTextTradeNoActiveFile] = "{0}, no tienes un archivo TextTrade activo. Sube uno primero con `{1}tt`.";
                target[LocalizationKeys.DiscordTextTradeInvalidSetNumber] = "Numero de set invalido. Usa `{0}tv 1` hasta `{0}tv {1}`.";
                target[LocalizationKeys.DiscordTextTradeViewTitle] = "Viendo set #{0}";
                target[LocalizationKeys.DiscordTextTradeViewFooter] = "Usa {0}tt {1} para tradear este Pokemon.";
                target[LocalizationKeys.DiscordBatchTradeLimit] = "Solo puedes procesar hasta {0} trades a la vez.\nReduce la cantidad de trades en tu lote.";
                target[LocalizationKeys.DiscordBatchTradeProcessing] = "{0} Procesando tu batch trade con {1} Pokemon...";
                target[LocalizationKeys.DiscordBatchTradeProcessingError] = "{0} Ocurrio un error al procesar tu batch trade. Intentalo de nuevo.";
                target[LocalizationKeys.DiscordBatchZipMissingAttachment] = "{0} Adjunta un `.zip`, `.rar` o `.7z` que contenga archivos PKM.";
                target[LocalizationKeys.DiscordBatchZipInvalidArchive] = "{0} Solo se aceptan archivos **.zip**, **.rar** y **.7z**.";
                target[LocalizationKeys.DiscordBatchZipDownloadFailed] = "{0} No se pudo descargar el archivo: {1}";
                target[LocalizationKeys.DiscordBatchZipProcessing] = "{0} Procesando tu archivo y extrayendo archivos...";
                target[LocalizationKeys.DiscordBatchZipLimit] = "{0} Incluiste {1} Pokemon, pero el limite es {2}.";
                target[LocalizationKeys.DiscordBatchZipNoValidPokemon] = "{0} Tu archivo no contenia ningun Pokemon valido.";
                target[LocalizationKeys.DiscordBatchZipUnexpectedError] = "{0} Ocurrio un error inesperado. Revisa los logs.";
                target[LocalizationKeys.DiscordEggTimeout] = "La generacion del huevo tomo demasiado tiempo y el bot agoto el tiempo de espera.";
                target[LocalizationKeys.DiscordEggFailed] = "No se pudo generar un huevo con el set enviado.\nIntenta quitar lineas posiblemente ilegales y vuelve a intentarlo.";
                target[LocalizationKeys.DiscordEggCreateFailed] = "Ups! No pude crear un huevo para eso.\nIntenta quitar lineas posiblemente ilegales y vuelve a intentarlo";
                target[LocalizationKeys.DiscordRequestProcessingError] = "Ocurrio un error al procesar la solicitud.";
                target[LocalizationKeys.DiscordLanguageNotRecognized] = "No pude reconocer el idioma: {0}.";
                target[LocalizationKeys.DiscordLegalizeTimeout] = "El set tomo demasiado tiempo en legalizarse.";
                target[LocalizationKeys.DiscordGenerateTimeout] = "Ese set tomo demasiado tiempo en generarse.";
                target[LocalizationKeys.DiscordCreateSomethingFailed] = "No pude crear algo con eso.";
                target[LocalizationKeys.DiscordBestAttemptDitto] = "⚠️ {1}, ups! {0} Aqui esta mi mejor intento para ese Ditto!";
                target[LocalizationKeys.DiscordDittoInvalidIVSpread] = "Spread de IV invalido: `{0}`. Cada valor debe estar entre 0 y 31.";
                target[LocalizationKeys.DiscordDittoUnknownArguments] = "{1}, argumento(s) de Ditto no reconocido(s): `{0}`. Usa IVs (por ejemplo, 31/31/31/31/31/31), idioma, naturaleza, Shiny, OT/TID/SID o juego de origen.";
                target[LocalizationKeys.DiscordDittoAdAttempt] = "{0} intento generar un Pokemon con publicidad en {1}.\nTodos rianse de el y llamenlo estupido.";
                target[LocalizationKeys.DiscordItemNotRecognized] = "{0}, no se reconocio el item que escribiste.";
                target[LocalizationKeys.DiscordBestAttemptSpecies] = "{0}\nAqui esta mi mejor intento para ese {1}!";
                break;
        }
    }
}
