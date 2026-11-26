using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddHudBotsPageTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // HUD: Bots page
                target[LocalizationKeys.BotsStart] = "START";
                target[LocalizationKeys.BotsStop] = "STOP";
                target[LocalizationKeys.BotsReboot] = "REBOOT";
                target[LocalizationKeys.BotsUpdate] = "UPDATE";
                target[LocalizationKeys.BotsReload] = "RELOAD";
                target[LocalizationKeys.BotsStartTooltip] = "Start all bots together that are listed.";
                target[LocalizationKeys.BotsStopTooltip] = "Stop all running bots together that are listed.";
                target[LocalizationKeys.BotsRebootTooltip] = "Reboot game and stop all bots listed.";
                target[LocalizationKeys.BotsUpdateTooltip] = "Check for program updates.";
                target[LocalizationKeys.BotsNewTooltip] = "Create a new bot slot.";
                target[LocalizationKeys.BotsReloadTooltip] = "Reload the application cleanly.";
                target[LocalizationKeys.BotsUpdateAvailableTooltip] = "Click to view update details and download the latest version.";
                target[LocalizationKeys.BotsNewRelease] = "NEW RELEASE!";
                target[LocalizationKeys.BotsGameModePlaceholder] = "Game";
                target[LocalizationKeys.BotsModeSwitchSuccessTitle] = "Mode Switch Successful";
                target[LocalizationKeys.BotsModeSwitchSuccessChanged] = "Game mode successfully changed to {0}!";
                target[LocalizationKeys.BotsModeSwitchSuccessReady] = "You can now start your bots and they will operate in the new mode.";
                target[LocalizationKeys.BotsModeSwitchErrorTitle] = "Mode Switch Error";
                target[LocalizationKeys.BotsModeSwitchReloadProgram] = "Please try reloading the program.";
                target[LocalizationKeys.BotsMainFormUnavailable] = "Main form instance not available. Please restart the program.";
                target[LocalizationKeys.BotsFailedSwitchMode] = "Failed to switch game mode: {0}";
                target[LocalizationKeys.BotsConfigFileNotFound] = "Config file not found at: {0}";
                target[LocalizationKeys.BotsFailedLoadConfig] = "Failed to load config for game mode: {0}";
                target[LocalizationKeys.BotsFailedRestart] = "Failed to restart: {0}";
                target[LocalizationKeys.BotsUpdateNowTo] = "Update now to {0}";
                break;
            case AppLanguage.Spanish:
                // HUD: Bots page
                target[LocalizationKeys.BotsStart] = "INICIAR";
                target[LocalizationKeys.BotsStop] = "DETENER";
                target[LocalizationKeys.BotsReboot] = "REINICIAR";
                target[LocalizationKeys.BotsUpdate] = "ACTUALIZAR";
                target[LocalizationKeys.BotsReload] = "RECARGAR";
                target[LocalizationKeys.BotsStartTooltip] = "Inicia juntos todos los bots listados.";
                target[LocalizationKeys.BotsStopTooltip] = "Detiene todos los bots en ejecucion listados.";
                target[LocalizationKeys.BotsRebootTooltip] = "Reinicia el juego y detiene todos los bots listados.";
                target[LocalizationKeys.BotsUpdateTooltip] = "Busca actualizaciones del programa.";
                target[LocalizationKeys.BotsNewTooltip] = "Crea un nuevo espacio de bot.";
                target[LocalizationKeys.BotsReloadTooltip] = "Recarga la aplicacion limpiamente.";
                target[LocalizationKeys.BotsUpdateAvailableTooltip] = "Haz clic para ver detalles de la actualizacion y descargar la ultima version.";
                target[LocalizationKeys.BotsNewRelease] = "NUEVA VERSION!";
                target[LocalizationKeys.BotsGameModePlaceholder] = "Juego";
                target[LocalizationKeys.BotsModeSwitchSuccessTitle] = "Modo cambiado correctamente";
                target[LocalizationKeys.BotsModeSwitchSuccessChanged] = "El modo de juego se cambio correctamente a {0}!";
                target[LocalizationKeys.BotsModeSwitchSuccessReady] = "Ahora puedes iniciar tus bots y funcionaran en el nuevo modo.";
                target[LocalizationKeys.BotsModeSwitchErrorTitle] = "Error al cambiar modo";
                target[LocalizationKeys.BotsModeSwitchReloadProgram] = "Intenta recargar el programa.";
                target[LocalizationKeys.BotsMainFormUnavailable] = "La ventana principal no esta disponible. Reinicia el programa.";
                target[LocalizationKeys.BotsFailedSwitchMode] = "No se pudo cambiar el modo de juego: {0}";
                target[LocalizationKeys.BotsConfigFileNotFound] = "No se encontro el archivo de configuracion en: {0}";
                target[LocalizationKeys.BotsFailedLoadConfig] = "No se pudo cargar la configuracion del modo de juego: {0}";
                target[LocalizationKeys.BotsFailedRestart] = "No se pudo reiniciar: {0}";
                target[LocalizationKeys.BotsUpdateNowTo] = "Actualizar ahora a {0}";
                break;
        }
    }
}
