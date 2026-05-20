using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordTradeModuleBatchHelpersTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: TradeModule BatchHelpers
                target[LocalizationKeys.DiscordBatchValidationFailedTitle] = "Batch Trade Validation Failed";
                target[LocalizationKeys.DiscordBatchValidationFailedDescription] = "{0} out of {1} Pokemon could not be processed.";
                target[LocalizationKeys.DiscordBatchValidationFailedSummaryDescription] = "Processed **{0}** requests:\n✅ **{1}** succeeded • ❌ **{2}** with errors.\n\nPlease fix the invalid sets and try again.";
                target[LocalizationKeys.DiscordBatchValidationFailedFooter] = "Please fix the invalid sets and try again.";
                target[LocalizationKeys.DiscordBatchTradeField] = "Trade #{0} - {1}";
                target[LocalizationKeys.DiscordBatchAdditionalErrorsField] = "...and more";
                target[LocalizationKeys.DiscordBatchAdditionalErrorsValue] = "There are **{0}** additional errors not shown to keep this message readable.";
                target[LocalizationKeys.DiscordUnknownSpecies] = "Unknown";
                target[LocalizationKeys.DiscordErrorLabel] = "Error";
                target[LocalizationKeys.DiscordHintLabel] = "Hint";
                target[LocalizationKeys.DiscordSetLabel] = "Set";
                break;
            case AppLanguage.Spanish:
                // Discord: TradeModule BatchHelpers
                target[LocalizationKeys.DiscordBatchValidationFailedTitle] = "Fallo la validacion del lote";
                target[LocalizationKeys.DiscordBatchValidationFailedDescription] = "{0} de {1} Pokemon no se pudieron procesar.";
                target[LocalizationKeys.DiscordBatchValidationFailedSummaryDescription] = "Se procesaron **{0}** solicitudes:\n✅ **{1}** correctas • ❌ **{2}** con errores.\n\nCorrige los sets invalidos e intentalo de nuevo.";
                target[LocalizationKeys.DiscordBatchValidationFailedFooter] = "Corrige los sets invalidos e intentalo de nuevo.";
                target[LocalizationKeys.DiscordBatchTradeField] = "Trade #{0} - {1}";
                target[LocalizationKeys.DiscordBatchAdditionalErrorsField] = "...y mas";
                target[LocalizationKeys.DiscordBatchAdditionalErrorsValue] = "Hay **{0}** errores adicionales no mostrados para mantener el mensaje legible.";
                target[LocalizationKeys.DiscordUnknownSpecies] = "Desconocido";
                target[LocalizationKeys.DiscordErrorLabel] = "Error";
                target[LocalizationKeys.DiscordHintLabel] = "Pista";
                target[LocalizationKeys.DiscordSetLabel] = "Set";
                break;
        }
    }
}
