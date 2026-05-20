using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordCloneModuleAndDumpModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: CloneModule and DumpModule
                target[LocalizationKeys.DiscordAlreadyInQueue] = "You already have an existing trade in the queue. Please wait until it is processed.";
                target[LocalizationKeys.DiscordAlreadyInQueueCannotClear] = "{0} You already have an existing trade in the queue that cannot be cleared. Please wait until it is processed.";
                target[LocalizationKeys.DiscordProcessingClone] = "Processing your clone request...";
                target[LocalizationKeys.DiscordPendingTrades] = "Pending Trades";
                target[LocalizationKeys.DiscordUsersWaiting] = "These are the users who are currently waiting:";
                break;
            case AppLanguage.Spanish:
                // Discord: CloneModule and DumpModule
                target[LocalizationKeys.DiscordAlreadyInQueue] = "Ya tienes un trade en la cola. Espera a que se procese.";
                target[LocalizationKeys.DiscordAlreadyInQueueCannotClear] = "{0} ya tienes un trade en la cola que no se puede limpiar. Espera a que se procese.";
                target[LocalizationKeys.DiscordProcessingClone] = "Procesando tu solicitud de clon...";
                target[LocalizationKeys.DiscordPendingTrades] = "Trades pendientes";
                target[LocalizationKeys.DiscordUsersWaiting] = "Estos son los usuarios que estan esperando:";
                break;
        }
    }
}
