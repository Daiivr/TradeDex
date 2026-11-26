using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddHudBotControllerCardsAndContextMenuTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // HUD: Bot controller cards and context menu
                target[LocalizationKeys.BotStatusDisconnected] = "DISCONNECTED";
                target[LocalizationKeys.BotStatusPaused] = "PAUSED";
                target[LocalizationKeys.BotStatusRunning] = "RUNNING";
                target[LocalizationKeys.BotStatusStopped] = "STOPPED";
                target[LocalizationKeys.BotStatusStopping] = "STOPPING";
                target[LocalizationKeys.BotStatusIdling] = "IDLING";
                target[LocalizationKeys.BotStatusIdle] = "IDLE";
                target[LocalizationKeys.BotStatusRebooting] = "REBOOTING";
                target[LocalizationKeys.BotStatusUnknown] = "UNKNOWN";
                target[LocalizationKeys.BotStatusError] = "ERROR";
                target[LocalizationKeys.BotUnknownConnection] = "Unknown Connection";
                target[LocalizationKeys.BotActions] = "Actions";
                target[LocalizationKeys.BotMenuStart] = "Start Bot";
                target[LocalizationKeys.BotMenuStop] = "Stop Bot";
                target[LocalizationKeys.BotMenuIdle] = "Idle Bot";
                target[LocalizationKeys.BotMenuResume] = "Resume Bot";
                target[LocalizationKeys.BotMenuRestart] = "Restart Bot";
                target[LocalizationKeys.BotMenuRebootStop] = "Reboot + Stop";
                target[LocalizationKeys.BotMenuScreenOn] = "Turn Screen On";
                target[LocalizationKeys.BotMenuScreenOff] = "Turn Screen Off";
                target[LocalizationKeys.BotMenuRemove] = "Remove Bot";
                target[LocalizationKeys.BotRestartConnectionPrompt] = "Restart the connection?";
                target[LocalizationKeys.BotUnsupportedCommand] = "Unsupported command.";
                target[LocalizationKeys.BotNotFound] = "Bot not found.";
                target[LocalizationKeys.BotRecoveryStatusTitle] = "Recovery Status";
                target[LocalizationKeys.BotRecoveryNotEnabled] = "Recovery service is not enabled for this bot.";
                target[LocalizationKeys.BotRecoveryStatusBody] = "Bot: {0}\nStatus: {1}\nRecovery Attempts: {2}\nTotal Crashes: {3}\nIs Recovering: {4}\n";
                break;
            case AppLanguage.Spanish:
                // HUD: Bot controller cards and context menu
                target[LocalizationKeys.BotStatusDisconnected] = "DESCONECTADO";
                target[LocalizationKeys.BotStatusPaused] = "PAUSADO";
                target[LocalizationKeys.BotStatusRunning] = "EJECUTANDO";
                target[LocalizationKeys.BotStatusStopped] = "DETENIDO";
                target[LocalizationKeys.BotStatusStopping] = "DETENIENDO";
                target[LocalizationKeys.BotStatusIdling] = "PASANDO A IDLE";
                target[LocalizationKeys.BotStatusIdle] = "IDLE";
                target[LocalizationKeys.BotStatusRebooting] = "REINICIANDO";
                target[LocalizationKeys.BotStatusUnknown] = "DESCONOCIDO";
                target[LocalizationKeys.BotStatusError] = "ERROR";
                target[LocalizationKeys.BotUnknownConnection] = "Conexion desconocida";
                target[LocalizationKeys.BotActions] = "Acciones";
                target[LocalizationKeys.BotMenuStart] = "Iniciar bot";
                target[LocalizationKeys.BotMenuStop] = "Detener bot";
                target[LocalizationKeys.BotMenuIdle] = "Pausar bot";
                target[LocalizationKeys.BotMenuResume] = "Reanudar bot";
                target[LocalizationKeys.BotMenuRestart] = "Reiniciar bot";
                target[LocalizationKeys.BotMenuRebootStop] = "Reiniciar + detener";
                target[LocalizationKeys.BotMenuScreenOn] = "Encender pantalla";
                target[LocalizationKeys.BotMenuScreenOff] = "Apagar pantalla";
                target[LocalizationKeys.BotMenuRemove] = "Eliminar bot";
                target[LocalizationKeys.BotRestartConnectionPrompt] = "Reiniciar la conexion?";
                target[LocalizationKeys.BotUnsupportedCommand] = "Comando no compatible.";
                target[LocalizationKeys.BotNotFound] = "Bot no encontrado.";
                target[LocalizationKeys.BotRecoveryStatusTitle] = "Estado de recuperacion";
                target[LocalizationKeys.BotRecoveryNotEnabled] = "El servicio de recuperacion no esta activado para este bot.";
                target[LocalizationKeys.BotRecoveryStatusBody] = "Bot: {0}\nEstado: {1}\nIntentos de recuperacion: {2}\nCierres inesperados totales: {3}\nRecuperandose: {4}\n";
                break;
        }
    }
}
