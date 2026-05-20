using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordMysteryEggModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: MysteryEggModule
                target[LocalizationKeys.DiscordMysteryEggUnavailable] = "Mystery Eggs are not available for Let's Go Pikachu/Eevee as the game does not support breeding.";
                target[LocalizationKeys.DiscordMysteryEggInvalidCount] = "Invalid number of eggs. Please specify between 1 and {0} eggs.";
                target[LocalizationKeys.DiscordMysteryEggGenerating] = "{0} Generating {1} Mystery Eggs...";
                target[LocalizationKeys.DiscordMysteryEggGenerateFailed] = "{0} Failed to generate any Mystery Eggs. Please try again.";
                target[LocalizationKeys.DiscordMysteryEggGenerateWarning] = "{0} Warning: failed to generate {1} Mystery Egg(s). Proceeding with {2}.";
                target[LocalizationKeys.DiscordMysteryEggBatchError] = "{0} An error occurred while processing your Mystery Egg batch. Please try again.";
                target[LocalizationKeys.DiscordMysteryEggBatchDisabled] = "Batch trades are currently disabled by the bot administrator, @{0}.";
                target[LocalizationKeys.DiscordMysteryEggBatchLimit] = "You can only request between 1 and {0} Mystery Eggs per batch.";
                target[LocalizationKeys.DiscordMysteryEggAlreadyInQueue] = "You are already in the queue!";
                target[LocalizationKeys.DiscordMysteryEggAddedBatch] = "{0} - Added batch of {1} Mystery Eggs to the queue! Position: {2}. Estimated: {3:F1} min(s).";
                target[LocalizationKeys.DiscordMysteryEggTitle] = "Mystery Egg {0} of {1}";
                target[LocalizationKeys.DiscordMysteryEggDescription] = "A mysterious egg containing a random Pokemon!";
                target[LocalizationKeys.DiscordMysteryEggFooter] = "Batch Trade {0} of {1}";
                target[LocalizationKeys.DiscordMysteryEggForUser] = "Mystery Egg for {0}";
                target[LocalizationKeys.DiscordMysteryEggLegalFailed] = "Failed to generate a legal mystery egg. Please try again later.";
                break;
            case AppLanguage.Spanish:
                // Discord: MysteryEggModule
                target[LocalizationKeys.DiscordMysteryEggUnavailable] = "Los Huevos Misteriosos no estan disponibles para Let's Go Pikachu/Eevee porque el juego no soporta crianza.";
                target[LocalizationKeys.DiscordMysteryEggInvalidCount] = "Numero de huevos invalido. Indica entre 1 y {0} huevos.";
                target[LocalizationKeys.DiscordMysteryEggGenerating] = "{0} Generando {1} Huevos Misteriosos...";
                target[LocalizationKeys.DiscordMysteryEggGenerateFailed] = "{0} No se pudo generar ningun Huevo Misterioso. Intentalo de nuevo.";
                target[LocalizationKeys.DiscordMysteryEggGenerateWarning] = "{0} Aviso: no se pudieron generar {1} Huevo(s) Misterioso(s). Continuando con {2}.";
                target[LocalizationKeys.DiscordMysteryEggBatchError] = "{0} Ocurrio un error al procesar tu lote de Huevos Misteriosos. Intentalo de nuevo.";
                target[LocalizationKeys.DiscordMysteryEggBatchDisabled] = "Los trades por lote estan desactivados por el administrador del bot, @{0}.";
                target[LocalizationKeys.DiscordMysteryEggBatchLimit] = "Solo puedes pedir entre 1 y {0} Huevos Misteriosos por lote.";
                target[LocalizationKeys.DiscordMysteryEggAlreadyInQueue] = "Ya estas en la cola!";
                target[LocalizationKeys.DiscordMysteryEggAddedBatch] = "{0} - Se agrego un lote de {1} Huevos Misteriosos a la cola! Posicion: {2}. Estimado: {3:F1} min.";
                target[LocalizationKeys.DiscordMysteryEggTitle] = "Huevo Misterioso {0} de {1}";
                target[LocalizationKeys.DiscordMysteryEggDescription] = "Un huevo misterioso con un Pokemon aleatorio!";
                target[LocalizationKeys.DiscordMysteryEggFooter] = "Trade por lote {0} de {1}";
                target[LocalizationKeys.DiscordMysteryEggForUser] = "Huevo Misterioso para {0}";
                target[LocalizationKeys.DiscordMysteryEggLegalFailed] = "No se pudo generar un huevo misterioso legal. Intentalo de nuevo mas tarde.";
                break;
        }
    }
}
