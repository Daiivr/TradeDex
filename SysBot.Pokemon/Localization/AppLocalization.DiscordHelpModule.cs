using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordHelpModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: HelpModule
                target[LocalizationKeys.DiscordHelpNoDescription] = "No description available.";
                target[LocalizationKeys.DiscordHelpInvalidPage] = "⚠️ Invalid page number. Please specify a number between 1 and {0}.";
                target[LocalizationKeys.DiscordHelpFooterPage] = "Page {0}/{1}";
                target[LocalizationKeys.DiscordHelpFooterNextPage] = " | Type `help {0}` for the next page.";
                target[LocalizationKeys.DiscordHelpTitle] = "Available Commands";
                target[LocalizationKeys.DiscordHelpDmSent] = "✅ {0}, I've sent you a DM with the help information!";
                target[LocalizationKeys.DiscordHelpDmDisabled] = "❌ {0}, I couldn't send you a DM because you have DMs disabled. Please enable DMs and try again.";
                target[LocalizationKeys.DiscordHelpDmError] = "❌ An error occurred while sending the DM: {0}";
                target[LocalizationKeys.DiscordHelpCommandNotFound] = "⚠️ Sorry, I couldn't find a command like **{0}**.";
                target[LocalizationKeys.DiscordHelpCommandTitle] = "Help for {0}";
                target[LocalizationKeys.DiscordHelpParametersLabel] = "Parameters";
                target[LocalizationKeys.DiscordHelpCommandDmSent] = "✉️ {0}, I've sent you a DM with the help information for the command **{1}**!";
                target[LocalizationKeys.DiscordHelpNoCommandsAvailable] = "⚠️ No commands are available for you right now.";
                target[LocalizationKeys.DiscordHelpCenterTitle] = "Help Center";
                target[LocalizationKeys.DiscordHelpDescription] = "Command index grouped by module. Use `help <command>` for details, parameters, and examples, or `ayuda` for guided tutorials.";
                target[LocalizationKeys.DiscordHelpTipFooter] = "Tip: use {0}help <command> for details or {0}ayuda for tutorials";
                target[LocalizationKeys.DiscordHelpModuleField] = "{0}";
                target[LocalizationKeys.DiscordHelpCommandAuthor] = "Command Help";
                target[LocalizationKeys.DiscordHelpCommandFooter] = "Use {0}help to see all commands";
                target[LocalizationKeys.DiscordHelpNoParameters] = "This command does not require parameters.";
                target[LocalizationKeys.DiscordHelpExampleLabel] = "Example";
                target[LocalizationKeys.DiscordHelpCommandField] = "{0}";
                target[LocalizationKeys.DiscordHelpOptionalParameter] = "(optional)";
                target[LocalizationKeys.DiscordTutorialDmSent] = "✅ {0}, I sent the tutorial `{1}` by DM.";
                target[LocalizationKeys.DiscordTutorialDmFailed] = "❌ {0}, I couldn't send you a DM. Enable direct messages or use `{1}ayuda` here.";
                break;
            case AppLanguage.Spanish:
                // Discord: HelpModule
                target[LocalizationKeys.DiscordHelpNoDescription] = "No hay descripcion disponible.";
                target[LocalizationKeys.DiscordHelpInvalidPage] = "⚠️ Numero de pagina invalido. Especifica un numero entre 1 y {0}.";
                target[LocalizationKeys.DiscordHelpFooterPage] = "Pagina {0}/{1}";
                target[LocalizationKeys.DiscordHelpFooterNextPage] = " | Escribe `help {0}` para ver la siguiente pagina.";
                target[LocalizationKeys.DiscordHelpTitle] = "Comandos disponibles";
                target[LocalizationKeys.DiscordHelpDmSent] = "✅ {0}, te envie un DM con la informacion de ayuda!";
                target[LocalizationKeys.DiscordHelpDmDisabled] = "❌ {0}, no pude enviarte un DM porque tienes los DMs desactivados. Activalos e intentalo de nuevo.";
                target[LocalizationKeys.DiscordHelpDmError] = "❌ Ocurrio un error al enviar el DM: {0}";
                target[LocalizationKeys.DiscordHelpCommandNotFound] = "⚠️ Lo siento, no pude encontrar un comando como **{0}**.";
                target[LocalizationKeys.DiscordHelpCommandTitle] = "Ayuda para {0}";
                target[LocalizationKeys.DiscordHelpParametersLabel] = "Parametros";
                target[LocalizationKeys.DiscordHelpCommandDmSent] = "✉️ {0}, te envie un DM con la informacion de ayuda para el comando **{1}**!";
                target[LocalizationKeys.DiscordHelpNoCommandsAvailable] = "⚠️ No hay comandos disponibles para ti en este momento.";
                target[LocalizationKeys.DiscordHelpCenterTitle] = "Centro de Ayuda";
                target[LocalizationKeys.DiscordHelpDescription] = "Indice de comandos organizado por modulo. Usa `help <comando>` para detalles, parametros y ejemplos, o `ayuda` para tutoriales guiados.";
                target[LocalizationKeys.DiscordHelpTipFooter] = "Consejo: usa {0}help <comando> para detalles o {0}ayuda para tutoriales";
                target[LocalizationKeys.DiscordHelpModuleField] = "{0}";
                target[LocalizationKeys.DiscordHelpCommandAuthor] = "Ayuda del comando";
                target[LocalizationKeys.DiscordHelpCommandFooter] = "Usa {0}help para ver todos los comandos";
                target[LocalizationKeys.DiscordHelpNoParameters] = "Este comando no requiere parametros.";
                target[LocalizationKeys.DiscordHelpExampleLabel] = "Ejemplo";
                target[LocalizationKeys.DiscordHelpCommandField] = "{0}";
                target[LocalizationKeys.DiscordHelpOptionalParameter] = "(opcional)";
                target[LocalizationKeys.DiscordTutorialDmSent] = "✅ {0}, te envie el tutorial `{1}` por MD.";
                target[LocalizationKeys.DiscordTutorialDmFailed] = "❌ {0}, no pude enviarte un MD. Activa tus mensajes directos o usa `{1}ayuda` aqui.";
                break;
        }
    }
}
