using System.Collections.Generic;

namespace SysBot.Pokemon.Localization;

public static partial class AppLocalization
{
    private static void AddLogsTradeModuleHelpersTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                target[LocalizationKeys.LogWc8ConvertToPkm] = "WC8 ConvertToPKM: file={0} valid={1} fateful={2} shiny={3}";
                target[LocalizationKeys.LogWc8ConvertToPkmError] = "WC8 ConvertToPKM error: {0}";
                target[LocalizationKeys.LogTradeModuleLegalityFail] = "TradeModule legality fail: species={0} form={1} loc={2} ot='{3}' shiny={4} shinyXor={5} result='{6}' | {7}";
                target[LocalizationKeys.LogZaNatureRequestedLegalApplied] = "{0}: Requested nature {1} is legal - applied.";
                target[LocalizationKeys.LogZaNatureRequestedIllegalMintApplied] = "{0}: Requested nature {1} is illegal for this encounter. Mint applied: Nature={2}, StatNature={3}.";
                target[LocalizationKeys.LogZaNatureRequestedIllegalMintRestricted] = "{0}: Requested nature {1} is illegal and minting is restricted for this encounter. Keeping forced Nature={2}, StatNature={3}.";
                target[LocalizationKeys.LogFullTradeErrorLogFailed] = "Failed to send Full Trade Error Log to channel {0}: {1}";
                target[LocalizationKeys.LogFullBatchTradeErrorLogFailed] = "Failed to send Full Batch Trade Error Log to channel {0}: {1}";
                target[LocalizationKeys.LogHomeFallbackSucceeded] = "{0}: HOME fallback succeeded from {1} (Version={2})";
                break;
            case AppLanguage.Spanish:
                target[LocalizationKeys.LogWc8ConvertToPkm] = "WC8 ConvertToPKM: archivo={0} valido={1} evento_fateful={2} shiny={3}";
                target[LocalizationKeys.LogWc8ConvertToPkmError] = "Error WC8 ConvertToPKM: {0}";
                target[LocalizationKeys.LogTradeModuleLegalityFail] = "Fallo de legalidad en TradeModule: especie={0} forma={1} ubicacion={2} ot='{3}' shiny={4} shinyXor={5} resultado='{6}' | {7}";
                target[LocalizationKeys.LogZaNatureRequestedLegalApplied] = "{0}: la naturaleza solicitada {1} es legal - aplicada.";
                target[LocalizationKeys.LogZaNatureRequestedIllegalMintApplied] = "{0}: la naturaleza solicitada {1} es ilegal para este encuentro. Menta aplicada: Nature={2}, StatNature={3}.";
                target[LocalizationKeys.LogZaNatureRequestedIllegalMintRestricted] = "{0}: la naturaleza solicitada {1} es ilegal y las mentas estan restringidas para este encuentro. Conservando Nature={2}, StatNature={3} forzadas.";
                target[LocalizationKeys.LogFullTradeErrorLogFailed] = "No se pudo enviar el log completo de error de trade al canal {0}: {1}";
                target[LocalizationKeys.LogFullBatchTradeErrorLogFailed] = "No se pudo enviar el log completo de error de batch trade al canal {0}: {1}";
                target[LocalizationKeys.LogHomeFallbackSucceeded] = "{0}: fallback de HOME exitoso desde {1} (Version={2})";
                break;
        }
    }
}
