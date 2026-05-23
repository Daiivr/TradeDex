using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordSlashCommandsTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: Slash commands
                target[LocalizationKeys.DiscordSlashServerOnly] = "❌ This command can only be used in a server.";
                target[LocalizationKeys.DiscordSlashPokemonEmpty] = "❌ Pokemon parameter is empty! Please use autocomplete to select a valid Pokemon.";
                target[LocalizationKeys.DiscordSlashPokemonInvalid] = "❌ Invalid Pokemon: **{0}**. Please use autocomplete to select a valid Pokemon.";
                target[LocalizationKeys.DiscordSlashGenerationUnknownError] = "❌ Unknown error occurred during Pokemon generation.";
                target[LocalizationKeys.DiscordSlashIllegalPokemon] = "❌ **Illegal Pokemon Detected**\n\n{0}";
                target[LocalizationKeys.DiscordSlashAlreadyInQueue] = "❌ You are already in the queue!";
                target[LocalizationKeys.DiscordSlashQueueFull] = "❌ The queue is currently full. Please try again later.";
                target[LocalizationKeys.DiscordSlashAddedQueueDm] = "✅ Pokemon added to queue! Check your DMs for the trade code.";
                target[LocalizationKeys.DiscordTradeBatchPrefix] = "Trade {0}/{1}: {2}";
                target[LocalizationKeys.DiscordAutocompleteNoMatches] = "No matches found";
                target[LocalizationKeys.DiscordPreconditionGuildRequired] = "Sorry {0}, this command can only be used in a server and not in direct messages.";
                target[LocalizationKeys.DiscordPreconditionQueueClosed] = "Sorry {0}, I am not currently accepting queue requests!";
                target[LocalizationKeys.DiscordPreconditionRoleRequired] = "{0} You do not have the required role to run this command.";
                target[LocalizationKeys.DiscordPreconditionSudoRequired] = "{0} You are not permitted to run this command.";
                target[LocalizationKeys.DiscordPreconditionOwnerRequired] = "⚠️ {0} only the owner of the bot can run this command.";
                target[LocalizationKeys.DiscordCommandTooFewParameters] = "⚠️ {0}, this command is missing required parameters. Include the required value and try again.";
                break;
            case AppLanguage.Spanish:
                // Discord: Slash commands
                target[LocalizationKeys.DiscordSlashServerOnly] = "❌ Este comando solo se puede usar en un servidor.";
                target[LocalizationKeys.DiscordSlashPokemonEmpty] = "❌ El parametro Pokemon esta vacio. Usa autocompletado para seleccionar un Pokemon valido.";
                target[LocalizationKeys.DiscordSlashPokemonInvalid] = "❌ Pokemon invalido: **{0}**. Usa autocompletado para seleccionar un Pokemon valido.";
                target[LocalizationKeys.DiscordSlashGenerationUnknownError] = "❌ Ocurrio un error desconocido durante la generacion del Pokemon.";
                target[LocalizationKeys.DiscordSlashIllegalPokemon] = "❌ **Pokemon ilegal detectado**\n\n{0}";
                target[LocalizationKeys.DiscordSlashAlreadyInQueue] = "❌ Ya estas en la cola!";
                target[LocalizationKeys.DiscordSlashQueueFull] = "❌ La cola esta llena. Intentalo de nuevo mas tarde.";
                target[LocalizationKeys.DiscordSlashAddedQueueDm] = "✅ Pokemon agregado a la cola! Revisa tus DMs para el codigo de trade.";
                target[LocalizationKeys.DiscordTradeBatchPrefix] = "Intercambio {0}/{1}: {2}";
                target[LocalizationKeys.DiscordAutocompleteNoMatches] = "No se encontraron coincidencias";
                target[LocalizationKeys.DiscordPreconditionGuildRequired] = "Lo siento {0}, este comando solo puede usarse dentro de un servidor y no en mensajes directos.";
                target[LocalizationKeys.DiscordPreconditionQueueClosed] = "Lo siento {0}, actualmente no acepto solicitudes para entrar en la cola.";
                target[LocalizationKeys.DiscordPreconditionRoleRequired] = "{0} No tienes el rol requerido para ejecutar este comando.";
                target[LocalizationKeys.DiscordPreconditionSudoRequired] = "{0} no estas autorizado a ejecutar este comando.";
                target[LocalizationKeys.DiscordPreconditionOwnerRequired] = "⚠️ {0} solo el dueno del bot puede ejecutar este comando.";
                target[LocalizationKeys.DiscordCommandTooFewParameters] = "⚠️ {0}, a este comando le faltan parametros requeridos. Incluye el valor necesario e intentalo de nuevo.";
                break;
        }
    }
}
