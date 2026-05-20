using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddHudMainWindowAndSharedDialogsTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // HUD: Main window and shared dialogs
                target[LocalizationKeys.NavBots] = "Bots";
                target[LocalizationKeys.NavHub] = "Hub";
                target[LocalizationKeys.NavLogs] = "Logs";
                target[LocalizationKeys.LanguageButtonEnglish] = "EN";
                target[LocalizationKeys.LanguageButtonSpanish] = "ES";
                target[LocalizationKeys.LanguageButtonTooltip] = "Switch language";
                target[LocalizationKeys.LanguageChangeBlockedBotRunning] = "A bot is currently running. Stop all bots before changing the language.";
                target[LocalizationKeys.Loading] = "LOADING ...";
                target[LocalizationKeys.DownloadFonts] = "Download Fonts";
                target[LocalizationKeys.DialogErrorTitle] = "Error";
                target[LocalizationKeys.DialogAlertTitle] = "Alert";
                target[LocalizationKeys.DialogPromptTitle] = "Prompt";
                target[LocalizationKeys.LogBotInitializationComplete] = "Bot initialization complete";
                target[LocalizationKeys.LogLanguageChanged] = "Language changed to {0}";
                break;
            case AppLanguage.Spanish:
                // HUD: Main window and shared dialogs
                target[LocalizationKeys.NavBots] = "Bots";
                target[LocalizationKeys.NavHub] = "Hub";
                target[LocalizationKeys.NavLogs] = "Registros";
                target[LocalizationKeys.LanguageButtonEnglish] = "EN";
                target[LocalizationKeys.LanguageButtonSpanish] = "ES";
                target[LocalizationKeys.LanguageButtonTooltip] = "Cambiar idioma";
                target[LocalizationKeys.LanguageChangeBlockedBotRunning] = "Hay un bot ejecutandose. Deten todos los bots antes de cambiar el idioma.";
                target[LocalizationKeys.Loading] = "CARGANDO ...";
                target[LocalizationKeys.DownloadFonts] = "Descargar fuentes";
                target[LocalizationKeys.DialogErrorTitle] = "Error";
                target[LocalizationKeys.DialogAlertTitle] = "Aviso";
                target[LocalizationKeys.DialogPromptTitle] = "Confirmar";
                target[LocalizationKeys.LogBotInitializationComplete] = "Inicializacion del bot completa";
                target[LocalizationKeys.LogLanguageChanged] = "Idioma cambiado a {0}";
                break;
        }
    }
}
