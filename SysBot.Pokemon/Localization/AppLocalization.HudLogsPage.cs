using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddHudLogsPageTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // HUD: Logs page
                target[LocalizationKeys.LogsNothingLogged] = "Nothing currently logged...";
                target[LocalizationKeys.LogsSearchPlaceholder] = "Search...";
                target[LocalizationKeys.LogsNext] = "NEXT";
                target[LocalizationKeys.LogsPrevious] = "PREV";
                target[LocalizationKeys.LogsClear] = "CLEAR";
                target[LocalizationKeys.LogsNoSearchTerm] = "No search term.";
                target[LocalizationKeys.LogsNoMatches] = "No matches.";
                target[LocalizationKeys.LogsMatchCount] = "Match {0} of {1}";
                target[LocalizationKeys.LogsEnterSearchTerm] = "Enter a search term.";
                target[LocalizationKeys.ContextCopy] = "Copy";
                target[LocalizationKeys.ContextClear] = "Clear";
                target[LocalizationKeys.ContextSelectAll] = "Select All";
                break;
            case AppLanguage.Spanish:
                // HUD: Logs page
                target[LocalizationKeys.LogsNothingLogged] = "No hay registros por ahora...";
                target[LocalizationKeys.LogsSearchPlaceholder] = "Buscar...";
                target[LocalizationKeys.LogsNext] = "SIG.";
                target[LocalizationKeys.LogsPrevious] = "ANT.";
                target[LocalizationKeys.LogsClear] = "LIMPIAR";
                target[LocalizationKeys.LogsNoSearchTerm] = "Sin termino de busqueda.";
                target[LocalizationKeys.LogsNoMatches] = "Sin coincidencias.";
                target[LocalizationKeys.LogsMatchCount] = "Coincidencia {0} de {1}";
                target[LocalizationKeys.LogsEnterSearchTerm] = "Escribe un termino de busqueda.";
                target[LocalizationKeys.ContextCopy] = "Copiar";
                target[LocalizationKeys.ContextClear] = "Limpiar";
                target[LocalizationKeys.ContextSelectAll] = "Seleccionar todo";
                break;
        }
    }
}
