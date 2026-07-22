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
                target[LocalizationKeys.LogConvertTrainerOverrideRequested] = "Convert TrainerOverride: requested OT={0} | requested TID={1} | requested SID={2} | requested OT gender={3} | species={4} | previous OT={5} | previous TID={6} | previous SID={7} | previous OT gender={8}";
                target[LocalizationKeys.LogConvertTrainerOverrideFinal] = "Convert TrainerOverride: final OT={0} | final TID={1} | final SID={2} | final OT gender={3} | legal={4}";
                target[LocalizationKeys.LogConvertTrainerOverrideSkipped] = "Convert TrainerOverride: skipped because AllowTrainerDataOverride is disabled. Ignoring requested OT={0} | TID={1} | SID={2} | species={3}";
                target[LocalizationKeys.LogConvertTrainerOverrideNotRequested] = "Convert TrainerOverride: no override was requested; ALM defaults remain. Override state={0}";
                target[LocalizationKeys.LogConvertTrainerOverrideReverted] = "Convert TrainerOverride: reverted because legality failed: {0}";
                target[LocalizationKeys.LogTradeTrainerOverrideRequested] = "Trade TrainerOverride: requested OT={0} | requested TID={1} | requested SID={2} | species={3} | previous OT={4} | previous TID={5} | previous SID={6}";
                target[LocalizationKeys.LogTradeTrainerOverrideFinal] = "Trade TrainerOverride: final OT={0} | final TID={1} | final SID={2} | legal={3}";
                target[LocalizationKeys.LogTradeTrainerOverrideSkipped] = "Trade TrainerOverride: skipped because AllowTrainerDataOverride is disabled. Ignoring requested OT={0} | TID={1} | SID={2} | species={3}";
                target[LocalizationKeys.LogTradeTrainerOverrideReverted] = "Trade TrainerOverride: reverted because legality failed: {0}";
                target[LocalizationKeys.LogTrainerGenderNone] = "none";
                target[LocalizationKeys.LogTrainerGenderMale] = "male";
                target[LocalizationKeys.LogTrainerGenderFemale] = "female";
                target[LocalizationKeys.LogBooleanTrue] = "true";
                target[LocalizationKeys.LogBooleanFalse] = "false";
                target[LocalizationKeys.LogTrainerOverrideStateNull] = "null";
                target[LocalizationKeys.LogTrainerOverrideStateEmpty] = "empty";
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
                target[LocalizationKeys.LogConvertTrainerOverrideRequested] = "Conversión TrainerOverride: OT solicitado={0} | TID solicitado={1} | SID solicitado={2} | género de OT solicitado={3} | especie={4} | OT anterior={5} | TID anterior={6} | SID anterior={7} | género de OT anterior={8}";
                target[LocalizationKeys.LogConvertTrainerOverrideFinal] = "Conversión TrainerOverride: OT final={0} | TID final={1} | SID final={2} | género de OT final={3} | legal={4}";
                target[LocalizationKeys.LogConvertTrainerOverrideSkipped] = "Conversión TrainerOverride: omitida porque AllowTrainerDataOverride está deshabilitado. Se ignora OT solicitado={0} | TID={1} | SID={2} | especie={3}";
                target[LocalizationKeys.LogConvertTrainerOverrideNotRequested] = "Conversión TrainerOverride: no se solicitó reemplazar los datos; se conservan los valores predeterminados de ALM. Estado del reemplazo={0}";
                target[LocalizationKeys.LogConvertTrainerOverrideReverted] = "Conversión TrainerOverride: revertida porque falló la legalidad: {0}";
                target[LocalizationKeys.LogTradeTrainerOverrideRequested] = "Intercambio TrainerOverride: OT solicitado={0} | TID solicitado={1} | SID solicitado={2} | especie={3} | OT anterior={4} | TID anterior={5} | SID anterior={6}";
                target[LocalizationKeys.LogTradeTrainerOverrideFinal] = "Intercambio TrainerOverride: OT final={0} | TID final={1} | SID final={2} | legal={3}";
                target[LocalizationKeys.LogTradeTrainerOverrideSkipped] = "Intercambio TrainerOverride: omitido porque AllowTrainerDataOverride está deshabilitado. Se ignora OT solicitado={0} | TID={1} | SID={2} | especie={3}";
                target[LocalizationKeys.LogTradeTrainerOverrideReverted] = "Intercambio TrainerOverride: revertido porque falló la legalidad: {0}";
                target[LocalizationKeys.LogTrainerGenderNone] = "ninguno";
                target[LocalizationKeys.LogTrainerGenderMale] = "masculino";
                target[LocalizationKeys.LogTrainerGenderFemale] = "femenino";
                target[LocalizationKeys.LogBooleanTrue] = "verdadero";
                target[LocalizationKeys.LogBooleanFalse] = "falso";
                target[LocalizationKeys.LogTrainerOverrideStateNull] = "nulo";
                target[LocalizationKeys.LogTrainerOverrideStateEmpty] = "vacío";
                break;
        }
    }
}
