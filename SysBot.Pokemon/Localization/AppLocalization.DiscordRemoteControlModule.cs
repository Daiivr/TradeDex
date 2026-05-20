using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordRemoteControlModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: RemoteControlModule
                target[LocalizationKeys.DiscordRemoteNoBotCommand] = "No bot is available to execute your command: {0}";
                target[LocalizationKeys.DiscordRemoteNoBotsConnected] = "No bots are currently connected.";
                target[LocalizationKeys.DiscordRemoteScreenAllSet] = "Screen state set to {0} for {1} out of {2} bots.";
                target[LocalizationKeys.DiscordRemoteNoBotIp] = "No bot has that IP address ({0}).";
                target[LocalizationKeys.DiscordRemoteUnknownButton] = "Unknown button value: {0}";
                target[LocalizationKeys.DiscordRemotePerformed] = "{0} has performed: {1}";
                target[LocalizationKeys.DiscordRemoteScreenSet] = "Screen state set to: {0}";
                target[LocalizationKeys.DiscordRemoteUnknownStick] = "Unknown stick: {0}";
                target[LocalizationKeys.DiscordRemoteStickReset] = "{0} has reset the stick position.";
                target[LocalizationKeys.DiscordStateOn] = "On";
                target[LocalizationKeys.DiscordStateOff] = "Off";
                break;
            case AppLanguage.Spanish:
                // Discord: RemoteControlModule
                target[LocalizationKeys.DiscordRemoteNoBotCommand] = "No hay ningun bot disponible para ejecutar tu comando: {0}";
                target[LocalizationKeys.DiscordRemoteNoBotsConnected] = "No hay bots conectados actualmente.";
                target[LocalizationKeys.DiscordRemoteScreenAllSet] = "Estado de pantalla cambiado a {0} para {1} de {2} bots.";
                target[LocalizationKeys.DiscordRemoteNoBotIp] = "Ningun bot tiene esa direccion IP ({0}).";
                target[LocalizationKeys.DiscordRemoteUnknownButton] = "Boton desconocido: {0}";
                target[LocalizationKeys.DiscordRemotePerformed] = "{0} ejecuto: {1}";
                target[LocalizationKeys.DiscordRemoteScreenSet] = "Estado de pantalla cambiado a: {0}";
                target[LocalizationKeys.DiscordRemoteUnknownStick] = "Stick desconocido: {0}";
                target[LocalizationKeys.DiscordRemoteStickReset] = "{0} reinicio la posicion del stick.";
                target[LocalizationKeys.DiscordStateOn] = "Encendido";
                target[LocalizationKeys.DiscordStateOff] = "Apagado";
                break;
        }
    }
}
