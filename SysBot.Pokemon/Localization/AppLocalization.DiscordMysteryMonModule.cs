using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordMysteryMonModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: MysteryMonModule
                target[LocalizationKeys.DiscordMysteryMonTryAgain] = "Please try to find your Mystery Pokemon again! Whatever it is, it's still waiting for you!";
                target[LocalizationKeys.DiscordMysteryMonBatchDisabled] = "Batch trades are currently disabled by the bot administrator, @{0}.";
                target[LocalizationKeys.DiscordMysteryMonBatchLimit] = "You can only request between 1 and {0} Mystery Pokemon per batch.";
                target[LocalizationKeys.DiscordMysteryMonGenerating] = "{0} Generating {1} Mystery Pokemon...";
                target[LocalizationKeys.DiscordMysteryMonGenerateFailed] = "{0} Failed to generate any Mystery Pokemon. Please try again.";
                target[LocalizationKeys.DiscordMysteryMonGenerateWarning] = "{0} Warning: failed to generate {1} Mystery Pokemon. Proceeding with {2}.";
                target[LocalizationKeys.DiscordMysteryMonBatchError] = "{0} An error occurred while processing your Mystery Mon batch. Please try again.";
                target[LocalizationKeys.DiscordMysteryMonBatchAuthor] = "Mystery Mon Batch Trade";
                break;
            case AppLanguage.Spanish:
                // Discord: MysteryMonModule
                target[LocalizationKeys.DiscordMysteryMonTryAgain] = "Intenta encontrar tu Pokemon Misterioso otra vez! Sea lo que sea, aun te esta esperando!";
                target[LocalizationKeys.DiscordMysteryMonBatchDisabled] = "Los trades por lote estan desactivados por el administrador del bot, @{0}.";
                target[LocalizationKeys.DiscordMysteryMonBatchLimit] = "Solo puedes pedir entre 1 y {0} Pokemon Misteriosos por lote.";
                target[LocalizationKeys.DiscordMysteryMonGenerating] = "{0} Generando {1} Pokemon Misteriosos...";
                target[LocalizationKeys.DiscordMysteryMonGenerateFailed] = "{0} No se pudo generar ningun Pokemon Misterioso. Intentalo de nuevo.";
                target[LocalizationKeys.DiscordMysteryMonGenerateWarning] = "{0} Aviso: no se pudieron generar {1} Pokemon Misteriosos. Continuando con {2}.";
                target[LocalizationKeys.DiscordMysteryMonBatchError] = "{0} Ocurrio un error al procesar tu lote de Mystery Mon. Intentalo de nuevo.";
                target[LocalizationKeys.DiscordMysteryMonBatchAuthor] = "Lote de Mystery Mon";
                break;
        }
    }
}
