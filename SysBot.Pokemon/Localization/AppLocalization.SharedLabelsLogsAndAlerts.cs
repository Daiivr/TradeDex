using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddSharedLabelsLogsAndAlertsTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Shared labels, logs, and alerts
                target[LocalizationKeys.CommonYes] = "Yes";
                target[LocalizationKeys.CommonNo] = "No";
                target[LocalizationKeys.LogStartingAllBots] = "Starting all bots...";
                target[LocalizationKeys.LogRestartingAllConsoles] = "Restarting all the consoles...";
                target[LocalizationKeys.AlertNoBotsStart] = "No bots configured, but all supporting services have been started.";
                target[LocalizationKeys.AlertNoBotsReboot] = "No bots configured, but all supporting services have been issued the reboot command.";
                break;
            case AppLanguage.Spanish:
                // Shared labels, logs, and alerts
                target[LocalizationKeys.CommonYes] = "Si";
                target[LocalizationKeys.CommonNo] = "No";
                target[LocalizationKeys.LogStartingAllBots] = "Iniciando todos los bots...";
                target[LocalizationKeys.LogRestartingAllConsoles] = "Reiniciando todas las consolas...";
                target[LocalizationKeys.AlertNoBotsStart] = "No hay bots configurados, pero todos los servicios de soporte se han iniciado.";
                target[LocalizationKeys.AlertNoBotsReboot] = "No hay bots configurados, pero se envio el comando de reinicio a todos los servicios de soporte.";
                break;
        }
    }
}
