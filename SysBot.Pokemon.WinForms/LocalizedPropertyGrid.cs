using SysBot.Pokemon.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace SysBot.Pokemon.WinForms;

internal static class LocalizedPropertyGrid
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        _registered = true;
        var pokemonAssembly = typeof(PokeTradeHubConfig).Assembly;
        foreach (var type in pokemonAssembly.GetTypes().Where(ShouldLocalizeType))
        {
            var provider = TypeDescriptor.GetProvider(type);
            TypeDescriptor.AddProviderTransparent(new LocalizedTypeDescriptionProvider(provider), type);
        }
    }

    public static void RefreshObject(object? value)
    {
        if (value is null)
            return;

        TypeDescriptor.Refresh(value);
        TypeDescriptor.Refresh(value.GetType());
    }

    private static bool ShouldLocalizeType(Type type)
    {
        if (!type.IsClass && !type.IsValueType)
            return false;

        var ns = type.Namespace;
        return ns is not null && ns.StartsWith("SysBot.Pokemon", StringComparison.Ordinal);
    }

    internal static string LocalizeDisplayName(string value, string propertyName)
    {
        if (AppLocalization.Language == AppLanguage.Spanish)
            return LocalizeToSpanish(value, propertyName, splitPropertyNameFallback: true);

        return LocalizeToEnglish(value, propertyName, splitPropertyNameFallback: true);
    }

    internal static string LocalizeCategory(string value)
    {
        if (AppLocalization.Language == AppLanguage.Spanish)
            return LocalizeToSpanish(value, value, splitPropertyNameFallback: true);

        return LocalizeToEnglish(value, value, splitPropertyNameFallback: true);
    }

    internal static string LocalizeDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (AppLocalization.Language == AppLanguage.Spanish)
            return EnglishToSpanishSettingsText.TryGetValue(value, out var spanish) ? spanish : value;

        if (SettingsText.TryGetValue(value, out var localized))
            return localized;

        var runtime = AppLocalization.LocalizeRuntimeMessage(value);
        return runtime == value ? TranslateSpanishSettingsTextToEnglish(value) : runtime;
    }

    internal static string LocalizeValueText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (AppLocalization.Language == AppLanguage.Spanish)
            return EnglishToSpanishSettingsText.TryGetValue(value, out var spanish) ? spanish : value;

        if (SettingsText.TryGetValue(value, out var english))
            return english;

        var runtime = AppLocalization.LocalizeRuntimeMessage(value);
        return runtime == value ? TranslateSpanishSettingsTextToEnglish(value) : runtime;
    }

    private static string LocalizeToEnglish(string value, string propertyName, bool splitPropertyNameFallback)
    {
        if (SettingsText.TryGetValue(value, out var localized))
            return localized;

        if (SettingsText.TryGetValue(propertyName, out localized))
            return localized;

        var splitPropertyName = SplitPascalCase(propertyName);
        if (SettingsText.TryGetValue(splitPropertyName, out localized))
            return localized;

        return splitPropertyNameFallback ? splitPropertyName : value;
    }

    private static string LocalizeToSpanish(string value, string propertyName, bool splitPropertyNameFallback)
    {
        if (EnglishToSpanishSettingsText.TryGetValue(value, out var localized))
            return localized;

        if (EnglishToSpanishSettingsText.TryGetValue(propertyName, out localized))
            return localized;

        var splitPropertyName = SplitPascalCase(propertyName);
        if (EnglishToSpanishSettingsText.TryGetValue(splitPropertyName, out localized))
            return localized;

        return splitPropertyNameFallback ? TranslateEnglishSettingNameToSpanish(SplitPascalCase(propertyName)) : value;
    }

    private static string TranslateEnglishSettingNameToSpanish(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (EnglishToSpanishSettingsText.TryGetValue(value, out var exact))
            return exact;

        foreach (var (source, replacement) in SpanishPhraseFallbacks)
            value = value.Replace(source, replacement, StringComparison.Ordinal);

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            var suffix = string.Empty;
            var word = words[i];
            while (word.Length > 0 && char.IsPunctuation(word[^1]))
            {
                suffix = word[^1] + suffix;
                word = word[..^1];
            }

            if (EnglishToSpanishWordFallbacks.TryGetValue(word, out var translated))
                words[i] = translated + suffix;
        }

        return string.Join(' ', words);
    }

    private static string TranslateSpanishSettingsTextToEnglish(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (SettingsText.TryGetValue(value, out var exact))
            return exact;

        foreach (var (source, replacement) in SpanishSettingsPhraseFallbacks)
            value = value.Replace(source, replacement, StringComparison.OrdinalIgnoreCase);

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            var prefix = string.Empty;
            var suffix = string.Empty;
            var word = words[i];

            while (word.Length > 0 && char.IsPunctuation(word[0]) && word[0] is not '[' and not '(' and not '{')
            {
                prefix += word[0];
                word = word[1..];
            }

            while (word.Length > 0 && char.IsPunctuation(word[^1]) && word[^1] is not ']' and not ')' and not '}')
            {
                suffix = word[^1] + suffix;
                word = word[..^1];
            }

            if (SpanishToEnglishSettingsWordFallbacks.TryGetValue(word, out var translated))
                words[i] = prefix + translated + suffix;
        }

        return string.Join(' ', words);
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var chars = new List<char>(value.Length + 8) { value[0] };
        for (int i = 1; i < value.Length; i++)
        {
            var previous = value[i - 1];
            var current = value[i];
            var next = i + 1 < value.Length ? value[i + 1] : '\0';

            if (char.IsUpper(current) && (char.IsLower(previous) || (next != '\0' && char.IsLower(next))))
                chars.Add(' ');

            chars.Add(current);
        }

        return NormalizeSettingAcronyms(new string(chars.ToArray()));
    }

    private static string NormalizeSettingAcronyms(string value) =>
        value
            .Replace("I Vs", "IVs", StringComparison.Ordinal)
            .Replace("E Vs", "EVs", StringComparison.Ordinal)
            .Replace("G Vs", "GVs", StringComparison.Ordinal)
            .Replace("A Vs", "AVs", StringComparison.Ordinal)
            .Replace("P K Ms", "PKMs", StringComparison.Ordinal)
            .Replace("PK Ms", "PKMs", StringComparison.Ordinal)
            .Replace("P K M", "PKM", StringComparison.Ordinal)
            .Replace("PKHe X", "PKHeX", StringComparison.Ordinal)
            .Replace("PK He X", "PKHeX", StringComparison.Ordinal)
            .Replace("U R L", "URL", StringComparison.Ordinal)
            .Replace("I D", "ID", StringComparison.Ordinal);

    private static readonly Dictionary<string, string> SettingsText = new(StringComparer.Ordinal)
    {
        ["Ajustes de configuración de Trade"] = "Trade Configuration Settings",
        ["Ajustes de configuración de trade"] = "Trade Configuration Settings",
        ["Ajustes de configuración de Trade Embed"] = "Trade Embed Settings",
        ["Configuración del Embed Trade"] = "Trade Embed Settings",
        ["Configuración del trade"] = "Trade Configuration",
        ["Código mínimo de trade"] = "Min Trade Code",
        ["Código máximo de trade"] = "Max Trade Code",
        ["Guardar códigos de trade"] = "Store Trade Codes",
        ["Tiempo de espera del trade"] = "Trade Wait Time",
        ["Tiempo máximo para confirmar trade"] = "Max Trade Confirm Time",
        ["Especie para Item Trade"] = "Item Trade Species",
        ["Item predeterminado"] = "Default Held Item",
        ["Sugerir movimientos reaprendibles"] = "Suggest Relearn Moves",
        ["Permitir trades por lotes"] = "Allow Batch Trades",
        ["Habilitar verificación de spam"] = "Enable Spam Check",
        ["Máximo de Pokemon por trade"] = "Max Pkms Per Trade",
        ["Máximo de dumps por trade"] = "Max Dumps Per Trade",
        ["Tiempo máximo para dump trade"] = "Max Dump Trade Time",
        ["Comprobar legalidad en dump trade"] = "Dump Trade Legality Check",
        ["Bloquear evolución por trade"] = "Disallow Trade Evolve",
        ["Retraso máximo de animación de trade"] = "Trade Animation Max Delay Seconds",
        ["Configuración de las estadísticas de recuento de trades"] = "Trade Count Statistics Settings",
        ["Estadísticas del recuento de trades"] = "Trade Count Statistics",
        ["Configuración de VGCPastes"] = "VGCPastes Settings",
        ["Ajustes de configuración de VGCPastes"] = "VGCPastes Configuration Settings",
        ["(Configuracion de Barra de Donaciones)"] = "Donation Progress Bar Settings",
        ["Configuraciones relacionadas con el estado del canal."] = "Channel Status Settings",
        ["Configuración de anuncios"] = "Announcement Settings",
        ["Configuración de integración de Discord"] = "Discord Integration Settings",
        ["Ajustes de configuración de la cola"] = "Queue Configuration Settings",
        ["Configuración de legalidad"] = "Legality Settings",
        ["Configuración de archivos"] = "File Settings",
        ["Configuración de distribución"] = "Distribution Settings",
        ["Configuración de favoritismo"] = "Favoritism Settings",
        ["Configuración de recuperación"] = "Recovery Settings",
        ["Configuracion de recuperacion de bots"] = "Bot Recovery Settings",
        ["Configuración comercial de distribución"] = "Distribution Trade Settings",
        ["Configuración de carpeta/dump"] = "Folder/Dump Settings",
        ["Archivos"] = "Files",
        ["Configuración de generación de legalidad"] = "Legality Generation Settings",
        ["Configuración de verificación de semillas"] = "Seed Check Settings",
        ["Configuración de condición de parada"] = "Stop Condition Settings",
        ["Configuración de transmisión"] = "Stream Settings",
        ["Configuración de integración de Twitch"] = "Twitch Integration Settings",
        ["Configuración de integración de YouTube"] = "YouTube Integration Settings",
        ["SysBot.Pokemon.WebServerSettings"] = "Web Server Settings",
        ["Configuración para unirse a la cola"] = "Queue Join Settings",
        ["Configuración de Tiempos"] = "Timing Settings",
        ["Configuración Variada"] = "Miscellaneous Settings",
        ["Tiempos Específicos de Incursiones"] = "Raid-Specific Timings",
        ["Configuración de Monitoreo de Abuso de Intercambios"] = "Trade Abuse Monitor Settings",
        ["Cualquiera permitido"] = "Anyone allowed",
        ["Ninguno permitido (ninguno especificado)."] = "None allowed (none specified).",

        ["BotTrade"] = "Bot Trade",
        ["BotEncounter"] = "Bot Encounter",
        ["Bot"] = "Bot",
        ["Integration"] = "Integration",
        ["Operation"] = "Operation",
        ["FeatureToggle"] = "Feature Toggle",
        ["Debug"] = "Debug",
        ["Channels"] = "Channels",
        ["Roles"] = "Roles",
        ["Emojis"] = "Emojis",
        ["Moderation"] = "Moderation",
        ["Servers"] = "Servers",
        ["Startup"] = "Startup",
        ["Users"] = "Users",
        ["Appearance"] = "Appearance",
        ["Generate"] = "Generate",
        ["Misc"] = "Misc",
        ["QueueToggle"] = "Queue Toggle",
        ["TimeBias"] = "Time Bias",
        ["UserBias"] = "User Bias",
        ["Distribute"] = "Distribute",
        ["Synchronize"] = "Synchronize",
        ["TradeConfig"] = "Trade Config",
        ["EmbedSettings"] = "Embed Settings",
        ["CountStats"] = "Count Stats",
        ["VGCPastesConfig"] = "VGCPastes Config",

        ["Apagar Pantalla"] = "Turn Off Screen",
        ["Usar Teclado?"] = "Use Keyboard",
        ["Modo Anti Suspenso"] = "Anti-Idle Mode",
        ["Habilitar registros?"] = "Enable Logs?",
        ["Maximo de Archivos de Registro"] = "Max Archive Files",
        ["Configuración de las estadísticas de recuento de trade"] = "Trade Count Statistics Settings",
        ["Texto adicional del embed"] = "Additional Embed Text",
        ["Canales de Anuncios"] = "Announcement Channels",
        ["Canales de Registro de Abusos"] = "Abuse Log Channels",
        ["Canales"] = "Channels",
        ["Ajustes de los Anuncios"] = "Announcement Settings",
        ["Ajustes del bot"] = "Bot Settings",
        ["Ajustes de canales"] = "Channel Settings",
        ["Ajustes de roles"] = "Role Settings",
        ["Moderacion"] = "Moderation",
        ["Ajustes de moderacion"] = "Moderation Settings",
        ["Estado de Juego del Bot"] = "Bot Game Status",
        ["Estado personalizado del bot"] = "Bot Custom Status",
        ["Sistema de XP"] = "XP System",
        ["Iconos de mensajes"] = "Message Icons",
        ["Insignias"] = "Badges",
        ["Ligas"] = "League Emojis",
        ["Ajustes de emojis"] = "Emoji Settings",
        ["Estado del canal"] = "Channel Status",
        ["Permitir cualquier prefijo"] = "Allow Any Prefix",
        ["Reenviar DMs"] = "Forward User DMs To Bot",
        ["Lista de Sudos Globales"] = "Global Sudo List",
        ["Canales de Registros"] = "Logging Channels",
        ["Opciones del Stream"] = "Stream Options",
        ["Opciones de Donacion"] = "Donation Options",
        ["Eliminacion de mensajes"] = "Message Deletion",
        ["Retraso para eliminar mensajes"] = "Message Delete Delay",
        ["Eliminar comandos de usuario"] = "Delete User Commands",
        ["Canal de log completo de errores de trade"] = "Full Trade Error Log Channel",
        ["Activar el estado del canal"] = "Enable Channel Status",
        ["Link al Stream"] = "Stream Link",
        ["Icono de la plataforma de Stream"] = "Stream Platform Icon",
        ["Link para Donaciones"] = "Donation Link",
        ["Configuracion Extra"] = "Extra Settings",
        ["Mostrar Barra de Progreso"] = "Show Progress Bar",
        ["Meta de Donaciones"] = "Donation Goal",
        ["Donaciones Actuales"] = "Current Donations",
        ["¿Mostrar Emojis de Movimientos?"] = "Show Move Emojis?",
        ["¿Mostrar Emojis de Tipo Tera?"] = "Show Tera Type Emojis?",
        ["¿Usar Emojis de Tamaño?"] = "Use Size Emojis?",
        ["Emoji de movimiento Plus"] = "Plus Move Emoji",
        ["Emojis de Género"] = "Gender Emojis",
        ["Emojis de Marcas y Estados Especiales"] = "Special Marks and Status Emojis",
        ["Emojis de Movimientos"] = "Move Emojis",
        ["Emojis de tamaño"] = "Size Emojis",
        ["Emojis de Tipo Tera"] = "Tera Type Emojis",
        ["Emojis Shiny"] = "Shiny Emojis",
        ["Contenido"] = "Content",
        ["Contenido mostrado"] = "Displayed Content",
        ["Ajustes generales del embed"] = "General Embed Settings",
        ["Ajustes de emojis del embed"] = "Embed Emoji Settings",
        ["Campos visibles del embed"] = "Displayed Embed Fields",
        ["Mostrar AVs para LGPE"] = "Show LGPE AVs",
        ["Mostrar GVs para PLA"] = "Show PLA GVs",
        ["Mostrar Rastreador"] = "Show Tracker",
        ["Mostrar Tamaño"] = "Show Size",
        ["Mostrar Tera Tipo"] = "Show Tera Type",
        ["Mostrar Nivel"] = "Show Level",
        ["Mostrar Ball"] = "Show Ball",
        ["Mostrar nivel de encuentro"] = "Show Met Level",
        ["Mostrar Fecha de Encuentro"] = "Show Met Date",
        ["Mostrar ubicacion de encuentro"] = "Show Met Location",
        ["Mostrar Habilidad"] = "Show Ability",
        ["Mostrar Naturaleza"] = "Show Nature",
        ["Mostrar Idioma"] = "Show Language",
        ["Mostrar IVs"] = "Show IVs",
        ["Mostrar EVs"] = "Show EVs",
        ["Emoji Escala XXXS"] = "XXXS Scale Emoji",
        ["Emoji Escala XXXL"] = "XXXL Scale Emoji",
        ["Emoji Shiny Square"] = "Square Shiny Emoji",
        ["Emoji Shiny Normal"] = "Star Shiny Emoji",
        ["Emoji Masculino"] = "Male Emoji",
        ["Emoji Femenino"] = "Female Emoji",
        ["Emoji de Regalo Misterioso"] = "Mystery Gift Emoji",
        ["Emoji de la Marca Alfa"] = "Alpha Mark Emoji",
        ["Emoji de la Marca Imbatible"] = "Mightiest Mark Emoji",
        ["Emoji de Alfa PLA"] = "PLA Alpha Emoji",
        ["Emoji de Gigantamax"] = "Gigantamax Emoji",
        ["No establecido"] = "Not Set",
        ["Use Keyboard"] = "Use Keyboard",
        ["Discord"] = "Discord",
        ["Token"] = "Token",
        ["Additional Embed Text"] = "Additional Embed Text",
        ["Allow Global Sudo"] = "Allow Global Sudo",
        ["Announcement Channels"] = "Announcement Channels",
        ["Abuse Log Channels"] = "Abuse Log Channels",
        ["Announcement Settings"] = "Announcement Settings",
        ["Bot Color Status Trade Only"] = "Bot Color Status Trade Only",
        ["Bot Embed Status"] = "Bot Embed Status",
        ["Channel Status Config"] = "Channel Status Config",
        ["Bot Game Status"] = "Bot Game Status",
        ["Enable XP System"] = "Enable XP System",
        ["Message Icons"] = "Message Icons",
        ["Emoji Settings"] = "Emoji Settings",
        ["Moderation Settings"] = "Moderation Settings",
        ["Custom Badge Emojis"] = "Custom Badge Emojis",
        ["League Emojis"] = "League Emojis",
        ["Channel Status"] = "Channel Status",
        ["Channel Whitelist"] = "Channel Whitelist",
        ["Command Prefix"] = "Command Prefix",
        ["Allow Any Prefix"] = "Allow Any Prefix",
        ["Convert PKM Reply Any Channel"] = "Convert PKM Reply Any Channel",
        ["Convert PKM To Showdown Set"] = "Convert PKM To Showdown Set",
        ["UserDMsToBotForwarder"] = "Forward User DMs To Bot",
        ["User DMs To Bot Forwarder"] = "Forward User DMs To Bot",
        ["Global Sudo List"] = "Global Sudo List",
        ["Hello Response"] = "Hello Response",
        ["Logging Channels"] = "Logging Channels",
        ["Stream"] = "Stream",
        ["Donation"] = "Donation",
        ["Module Blacklist"] = "Module Blacklist",
        ["Offline Emoji"] = "Offline Emoji",
        ["Online Emoji"] = "Online Emoji",
        ["Reply Cannot Use Command In Channel"] = "Reply When Commands Are Not Allowed",
        ["Reply To Thanks"] = "Reply To Thanks",
        ["Return PKMs"] = "Return PKMs",
        ["Message Deletion Enabled"] = "Message Deletion Enabled",
        ["Error Message Delete Delay Seconds"] = "Error Message Delete Delay Seconds",
        ["Delete User Command Messages"] = "Delete User Command Messages",
        ["Role Can Clone"] = "Role Can Clone",
        ["Role Can Dump"] = "Role Can Dump",
        ["Role Can Fix OT"] = "Role Can Fix OT",
        ["Role Can Seed Checkor Special Request"] = "Role Can Seed Check or Special Request",
        ["Role Can Trade"] = "Role Can Trade",
        ["Role Can Trade Plus"] = "Role Can Trade Plus",
        ["Role Favored"] = "Role Favored",
        ["Role Remote Control"] = "Role Remote Control",
        ["Role Sudo"] = "Role Sudo",
        ["Server Blacklist"] = "Server Blacklist",
        ["Trade Starting Channels"] = "Trade Starting Channels",
        ["Full Trade Error Log Channels"] = "Full Trade Error Log Channels",
        ["User Blacklist"] = "User Blacklist",
        ["Warning"] = "Warning",
        ["Success"] = "Success",
        ["Error"] = "Error",
        ["Direct Message"] = "Direct Message",
        ["Waiting"] = "Waiting",
        ["Enable Channel Status"] = "Enable Channel Status",
        ["Stream Link"] = "Stream Link",
        ["Stream Icon"] = "Stream Icon",
        ["Donation Link"] = "Donation Link",
        ["Progress Bar"] = "Progress Bar",
        ["Show Progress Bar"] = "Show Progress Bar",
        ["Donation Goal"] = "Donation Goal",
        ["Donation Current"] = "Current Donations",
        ["Announcement Embed Color"] = "Announcement Embed Color",
        ["Announcement Thumbnail Option"] = "Announcement Thumbnail Option",
        ["Custom Announcement Thumbnail Url"] = "Custom Announcement Thumbnail URL",
        ["Random Announcement Color"] = "Random Announcement Color",
        ["Random Announcement Thumbnail"] = "Random Announcement Thumbnail",
        ["Bot Name"] = "Bot Name",
        ["Bot Logo Image"] = "Bot Logo Image",
        ["Activado"] = "Enabled",
        ["Texto"] = "Text",
        ["Distribution"] = "Distribution",
        ["Favoritism"] = "Favoritism",
        ["Queues"] = "Queues",
        ["Stop Conditions"] = "Stop Conditions",
        ["Timings"] = "Timings",
        ["Trade"] = "Trade",
        ["Trade Abuse"] = "Trade Abuse",
        ["Twitch"] = "Twitch",
        ["You Tube"] = "YouTube",
        ["Recovery"] = "Recovery",
        ["Web Server"] = "Web Server",
        ["Folder"] = "Folder",
        ["Legality"] = "Legality",

        ["Informacion del emoji para mostrar movimientos Plus aplicables en el embed de Discord."] = "Emoji information used to show applicable Plus moves in the Discord embed.",
        ["Mostrará los iconos de tipo de movimiento junto a los movimientos en el Embed Trade (sólo Discord). Requiere que el usuario suba los emojis a su servidor."] = "Shows move type icons next to moves in the Discord trade embed. Requires the emojis to be uploaded to the server.",
        ["Mostrará los iconos de Tera Tipo junto a los movimientos en el Embed Trade (sólo Discord). Requiere que el usuario suba los emojis a su servidor."] = "Shows Tera type icons in the Discord trade embed. Requires the emojis to be uploaded to the server.",
        ["Si es verdadero, se mostrarán los emojis para las escalas XXXS y XXXL en el Embed Trade."] = "When enabled, shows emojis for XXXS and XXXL sizes in the trade embed.",
        ["Información personalizada de Emoji para los tipos de movimiento."] = "Custom emoji information for move types.",
        ["Configuración de emojis para todos los tipos Tera, incluyendo 'Stellar'."] = "Emoji settings for all Tera types, including Stellar.",
        ["Configuración de emojis para las escalas XXXS y XXXL."] = "Emoji settings for XXXS and XXXL sizes.",
        ["Configuración de emojis para Pokémon Shiny."] = "Emoji settings for shiny Pokémon.",
        ["Configuración de emojis para géneros."] = "Emoji settings for genders.",
        ["Configuración de emojis para marcas especiales y estados."] = "Emoji settings for special marks and states.",
        ["Activa o desactiva la barra de progreso de donaciones."] = "Enables or disables the donation progress bar.",
        ["Activar o desactivar los trades por lotes."] = "Enables or disables batch trades.",
        ["Almacenar y reutilizar códigos de Tradeo"] = "Store and reuse trade codes.",
        ["Apaga la pantalla de la Switch durante las operaciones"] = "Turns off the Switch screen during operations.",
        ["Canales que registrarán mensajes especiales, como anuncios."] = "Channels that will log special messages, such as announcements.",
        ["Canales que registrarán los mensajes de abusos."] = "Channels that will log abuse messages.",
        ["Canales de registro que registrarán mensajes de inicio de operaciones."] = "Log channels that will record trade start messages.",
        ["Ajustes principales del bot de Discord."] = "Main Discord bot settings.",
        ["Ajustes de canales usados por el bot en Discord."] = "Channel settings used by the bot in Discord.",
        ["Ajustes de roles que controlan acceso y prioridad en Discord."] = "Role settings that control access and priority in Discord.",
        ["Ajustes de emojis usados por el bot en Discord."] = "Emoji settings used by the bot in Discord.",
        ["Ajustes de listas negras, sudo y manejo de mensajes en Discord."] = "Blacklist, sudo, and message handling settings in Discord.",
        ["Los canales con estos ID son los únicos canales donde el bot reconoce comandos."] = "Channels with these IDs are the only channels where the bot acknowledges commands.",
        ["ID de usuario o de canal al que se reenviarán los DMs del bot. Déjalo vacío para desactivar."] = "User ID or channel ID where bot DMs will be forwarded. Leave empty to disable.",
        ["ID de canal que harán eco de los datos del bot de registro."] = "Channel IDs that will echo the log bot data.",
        ["Canal que registrara informacion detallada de errores de trade, incluyendo solicitudes del usuario y razones de fallo."] = "Channel that will log detailed trade error information, including user requests and failure reasons.",
        ["Deshabilitar esto eliminará la compatibilidad global con sudo."] = "Disabling this removes global sudo support.",
        ["ID de usuario de Discord separados por comas que tendrán acceso sudo al Bot Hub."] = "Comma-separated Discord user IDs that will have sudo access to the Bot Hub.",
        ["Mensaje personalizado con el que el bot responderá cuando un usuario lo salude. Utilice formato de cadena para mencionar al usuario en la respuesta."] = "Custom message the bot will use when replying to a user greeting. Use string formatting to mention the user in the response.",
        ["Lista de módulos que no se cargarán cuando se inicie el bot (separados por comas)."] = "List of modules that will not load when the bot starts, separated by commas.",
        ["Responde a los usuarios si no se les permite utilizar un comando determinado en el canal. Cuando es falso, el bot los ignorará silenciosamente."] = "Replies to users when they are not allowed to use a command in the channel. When false, the bot silently ignores them.",
        ["Enviará una respuesta aleatoria a un usuario que agradezca al bot."] = "Sends a random reply to a user who thanks the bot.",
        ["Devuelve al usuario los archivos PKM de Pokémon mostrados en el intercambio."] = "Returns the PKM files for Pokémon shown in the trade to the user.",
        ["Numero de segundos a esperar antes de eliminar mensajes de error/respuesta del bot. Solo aplica si MessageDeletionEnabled esta en true."] = "Number of seconds to wait before deleting bot error/reply messages. Only applies when MessageDeletionEnabled is true.",
        ["Los servidores con estos ID no podrán utilizar el bot abandonará el servidor."] = "Servers with these IDs cannot use the bot; the bot will leave the server.",
        ["Los usuarios con estos ID de usuario no pueden utilizar el bot."] = "Users with these user IDs cannot use the bot.",
        ["Emojis que se usan en mensajes visibles de Discord. Si un valor se deja vacio, se usa el icono predeterminado."] = "Emojis used in visible Discord messages. If a value is empty, the default icon is used.",
        ["Lista de emojis personalizados para insignias que se entregan al usuario luego de completar cierta cantidad de trades."] = "List of custom badge emojis awarded after users complete certain trade counts.",
        ["Lista de ligas del perfil segun el nivel de XP del usuario. Puedes usar emojis normales o emojis custom de Discord como <:bronze:1234567890>."] = "List of profile leagues based on the user's XP level. You can use normal emojis or custom Discord emojis like <:bronze:1234567890>.",
        ["Emoji personalizado para usar cuando el bot está offline."] = "Custom emoji to use when the bot is offline.",
        ["Emoji personalizado para usar cuando el bot está online."] = "Custom emoji to use when the bot is online.",
        ["Emoji personalizado para usar cuando el bot esta offline."] = "Custom emoji to use when the bot is offline.",
        ["Emoji personalizado para usar cuando el bot esta online."] = "Custom emoji to use when the bot is online.",
        ["Habilita o deshabilita el sistema de XP para usuarios cuando usan comandos."] = "Enables or disables the XP system for users when they use commands.",
        ["Enviará un estado embed para cuando el bot este online/offline a todos los canales incluidos en la lista blanca."] = "Sends an online/offline status embed to every whitelisted channel.",
        ["Estado personalizado del bot."] = "Custom bot status.",
        ["Texto que se mostrara como estado personalizado del bot, junto al avatar."] = "Text shown as the bot custom status, next to the avatar.",
        ["Token de inicio de sesión del bot."] = "Bot login token.",
        ["Texto adicional para agregar al comienzo del Embed."] = "Additional text to add at the beginning of the embed.",
        ["Cuando esta habilitado, el bot cancelara automaticamente un trade si le ofrecen un Pokemon que va a evolucionar."] = "When enabled, the bot automatically cancels a trade if it is offered a Pokemon that will evolve.",
        ["Cuando esta habilitado, el bot eliminara automaticamente mensajes de error y comandos de usuario despues de un retraso. Deshabilitalo para conservar todos los mensajes permanentemente."] = "When enabled, the bot automatically deletes error messages and user commands after a delay. Disable it to keep all messages permanently.",
        ["Cuando esta habilitado, envia una notificacion embed a los canales de anuncios cuando la cola se cierra por capacidad maxima."] = "When enabled, sends an embed notification to announcement channels when the queue closes due to maximum capacity.",
        ["Cuando esta habilitado, los mensajes de comandos del usuario se eliminaran junto con las respuestas del bot. Deshabilitalo para mantener visibles los comandos."] = "When enabled, user command messages are deleted along with the bot replies. Disable it to keep commands visible.",
        ["Cuando este habilitado, el bot ingresará el código comercial del trade a través del teclado (más rápido)."] = "When enabled, the bot enters the trade code through the keyboard, which is faster.",
        ["Cuando esté habilitado, el bot ingresará el código comercial del trade a través del teclado (más rápido)."] = "When enabled, the bot enters the trade code through the keyboard, which is faster.",
        ["Cuando esté habilitado, el bot permitirá a los usuarios enviar comandos mediante susurros (evite el modo lento)"] = "When enabled, users can send commands by whisper to avoid slow mode.",
        ["Cuando esté habilitado, el bot procesará los comandos enviados al canal."] = "When enabled, the bot processes commands sent to the channel.",
        ["Permite a los usuarios salir de la cola mientras se intercambian."] = "Allows users to leave the queue while they are being processed.",
        ["Alterna si los usuarios pueden unirse a la cola."] = "Toggles whether users can join the queue.",
        ["Evita agregar usuarios si ya hay tantos usuarios en la cola."] = "Prevents adding users when the queue already has this many users.",
        ["Determina cuándo se activa y desactiva la cola."] = "Determines when the queue turns on and off.",
        ["Ajustes relacionados con la configuración del trade."] = "Settings related to trade configuration.",
        ["Ajustes relacionados con el Trade Embed en Discord."] = "Settings related to the Discord trade embed.",
        ["Ajustes relacionados con las estadísticas de recuento de trades."] = "Settings related to trade count statistics.",
        ["Ajustes relacionados con la Configuración de VGCPastes."] = "Settings related to VGCPastes configuration.",
    };

    private static readonly Dictionary<string, string> CuratedSpanishSettingNames = new(StringComparer.Ordinal)
    {
        ["Anti Idle"] = "Anti inactividad",
        ["Logging Enabled"] = "Registros activados",
        ["Max Archive Files"] = "Máximo de archivos archivados",
        ["Mode"] = "Modo",
        ["Skip Console Bot Creation"] = "Omitir creación de bots en consola",
        ["Use Keyboard"] = "Usar teclado",

        ["Distribute Folder"] = "Carpeta de Distribución",
        ["Dump"] = "Dump",
        ["Dump Folder"] = "Carpeta de Dump",
        ["HOME Ready PKM Folder"] = "Carpeta PKM lista para HOME",
        ["PKHeX Directory"] = "Directorio de PKHeX",
        ["PKHe X Directory"] = "Directorio de PKHeX",
        ["Switch Remote For PC"] = "Switch Remote para PC",

        ["Distribute While Idle"] = "Distribuir mientras está inactivo",
        ["Ledy Quit If No Match"] = "Salir de Ledy si no hay coincidencia",
        ["Ledy Species"] = "Especie de Ledy",
        ["Random Code"] = "Código aleatorio",
        ["Remain In Union Room BDSP"] = "Permanecer en la sala Unión de BDSP",
        ["Shuffled"] = "Aleatorio",
        ["Synchronize Bots"] = "Sincronizar bots",
        ["Synchronize Delay Barrier"] = "Retraso de barrera para sincronización",
        ["Synchronize Timeout"] = "Tiempo máximo de sincronización",
        ["Trade Code"] = "Código de trade",

        ["Allow Batch Commands"] = "Permitir comandos por lote",
        ["Allow Trainer Data Override"] = "Permitir sobrescribir datos del entrenador",
        ["Disallow Non Natives"] = "Bloquear Pokémon no nativos",
        ["Disallow Tracked"] = "Bloquear Pokémon con tracker",
        ["Enable Easter Eggs"] = "Activar easter eggs",
        ["Enable HOME Tracker Check"] = "Activar comprobación de tracker HOME",
        ["Force Level100for50"] = "Forzar nivel 100 para nivel 50",
        ["Force Specified Ball"] = "Forzar Ball especificada",
        ["Game Version Priority"] = "Prioridad de versión del juego",
        ["Generate Language"] = "Idioma de generación",
        ["Generate Path Trainer Info"] = "Generar ruta de datos del entrenador",
        ["Generate SID16"] = "Generar SID16",
        ["Generate TID16"] = "Generar TID16",
        ["MGDB Path"] = "Ruta de MGDB",
        ["Prioritize Encounters"] = "Priorizar encuentros",
        ["Prioritize Game"] = "Priorizar juego",
        ["Priority Order"] = "Orden de prioridad",
        ["Reset HOME Tracker"] = "Reiniciar tracker HOME",
        ["Set All Legal Ribbons"] = "Agregar todas las cintas legales",
        ["Set Battle Version"] = "Asignar versión de combate",
        ["Set Matching Balls"] = "Asignar Balls coincidentes",
        ["Timeout"] = "Tiempo máximo",
        ["Use Trade Partner Info"] = "Usar datos del compañero de trade",

        ["Can Dequeue If Processing"] = "Permitir salir de la cola durante el procesamiento",
        ["Can Queue"] = "Permitir unirse a la cola",
        ["Estimated Delay Factor"] = "Factor de retraso estimado",
        ["Flex Mode"] = "Modo Flex",
        ["Interval Close For"] = "Intervalo de cierre",
        ["Interval Open For"] = "Intervalo de apertura",
        ["Max Queue Count"] = "Máximo de usuarios en cola",
        ["Notify On Queue Close"] = "Notificar al cerrar la cola",
        ["Queue Toggle Mode"] = "Modo de control de cola",
        ["Threshold Lock"] = "Umbral para cerrar cola",
        ["Threshold Unlock"] = "Umbral para abrir cola",
        ["Yield Mult Count Clone"] = "Multiplicador por cantidad para Clone",
        ["Yield Mult Count Dump"] = "Multiplicador por cantidad para Dump",
        ["Yield Mult Count Fix OT"] = "Multiplicador por cantidad para Fix OT",
        ["Yield Mult Count Seed Check"] = "Multiplicador por cantidad para Seed Check",
        ["Yield Mult Count Trade"] = "Multiplicador por cantidad para Trade",
        ["Yield Mult Wait"] = "Multiplicador por espera",
        ["Yield Mult Wait Clone"] = "Multiplicador por espera para Clone",
        ["Yield Mult Wait Dump"] = "Multiplicador por espera para Dump",
        ["Yield Mult Wait Fix OT"] = "Multiplicador por espera para Fix OT",
        ["Yield Mult Wait Seed Check"] = "Multiplicador por espera para Seed Check",
        ["Yield Mult Wait Trade"] = "Multiplicador por espera para Trade",

        ["Enable Recovery"] = "Activar recuperación",
        ["Max Recovery Attempts"] = "Máximo de intentos de recuperación",
        ["Initial Recovery Delay Seconds"] = "Retraso inicial de recuperación (segundos)",
        ["Max Recovery Delay Seconds"] = "Retraso máximo de recuperación (segundos)",
        ["Backoff Multiplier"] = "Multiplicador de espera",
        ["Crash History Window Minutes"] = "Ventana de historial de cierres (minutos)",
        ["Max Crashes In Window"] = "Máximo de cierres en la ventana",
        ["Recover Intentional Stops"] = "Recuperar paradas intencionales",
        ["Successful Recovery Reset Delay Seconds"] = "Retraso para reiniciar recuperación exitosa (segundos)",
        ["Notify On Recovery Attempt"] = "Notificar intento de recuperación",
        ["Notify On Recovery Failure"] = "Notificar fallo de recuperación",
        ["Minimum Stable Uptime Seconds"] = "Tiempo mínimo estable activo (segundos)",

        ["Result Display Mode"] = "Modo de visualización de resultados",
        ["Show All Z3 Results"] = "Mostrar todos los resultados Z3",

        ["Capture Video Clip"] = "Capturar video",
        ["Extra Time Wait Capture Video"] = "Tiempo extra para capturar video",
        ["Mark Only"] = "Solo marcas",
        ["Match Found Echo Mention"] = "Mención al encontrar coincidencia",
        ["Match Shiny And IV"] = "Coincidir shiny e IV",
        ["Shiny Target"] = "Objetivo shiny",
        ["Stop On Form"] = "Detener por forma",
        ["Stop On Species"] = "Detener por especie",
        ["Target Max IVs"] = "IVs máximos objetivo",
        ["Target Min IVs"] = "IVs mínimos objetivo",
        ["Target Nature"] = "Naturaleza objetivo",
        ["Unwanted Marks"] = "Marcas no deseadas",

        ["Control Panel Port"] = "Puerto del panel de control",
        ["Enable Web Server"] = "Activar servidor web",
        ["Allow External Connections"] = "Permitir conexiones externas",
        ["DiscordOAuthClientId"] = "Discord OAuth Client ID",
        ["Discord OAuth Client Id"] = "Discord OAuth Client ID",
        ["Discord O Auth Client Id"] = "Discord OAuth Client ID",
        ["DiscordOAuthClientSecret"] = "Discord OAuth Client Secret",
        ["Discord OAuth Client Secret"] = "Discord OAuth Client Secret",
        ["Discord O Auth Client Secret"] = "Discord OAuth Client Secret",
        ["DiscordOAuthRedirectUri"] = "Discord OAuth Redirect URI",
        ["Discord OAuth Redirect Uri"] = "Discord OAuth Redirect URI",
        ["Discord O Auth Redirect Uri"] = "Discord OAuth Redirect URI",
        ["AdminID"] = "Admin ID",

        ["Log URL"] = "URL de log",
        ["Log Url"] = "URL de log",
        ["Room Id"] = "ID de sala",
        ["Channel ID"] = "ID del canal",
        ["Client ID"] = "ID del cliente",
        ["Client Secret"] = "Secreto del cliente",
        ["Message Start"] = "Inicio del mensaje",
        ["Sudo List"] = "Lista de sudos",

        ["Allow Commands Via Channel"] = "Permitir comandos por canal",
        ["Allow Commands Via Whisper"] = "Permitir comandos por susurro",
        ["Channel"] = "Canal",
        ["Discord Link"] = "Link de Discord",
        ["Distribution Count Down"] = "Cuenta regresiva de distribución",
        ["Notify Destination"] = "Destino de notificaciones",
        ["Throttle Messages"] = "Límite de mensajes",
        ["Throttle Seconds"] = "Segundos del límite",
        ["Throttle Whispers"] = "Límite de susurros",
        ["Throttle Whispers Seconds"] = "Segundos del límite de susurros",
        ["Trade Canceled Destination"] = "Destino de trade cancelado",
        ["Trade Finish Destination"] = "Destino de trade finalizado",
        ["Trade Search Destination"] = "Destino de búsqueda de trade",
        ["Trade Start Destination"] = "Destino de inicio de trade",
        ["Tutorial Link"] = "Link del tutorial",
        ["Tutorial Text"] = "Texto del tutorial",
        ["Username"] = "Usuario",

        ["Completed Trades Format"] = "Formato de trades completados",
        ["Copy Image File"] = "Copiar archivo de imagen",
        ["Create Assets"] = "Crear assets",
        ["Create Completed Trades"] = "Crear trades completados",
        ["Create Estimated Time"] = "Crear tiempo estimado",
        ["Create On Deck"] = "Crear On Deck",
        ["Create On Deck2"] = "Crear On Deck 2",
        ["Create Sprite File"] = "Crear archivo de sprite",
        ["Create Trade Start"] = "Crear inicio de trade",
        ["Create Trade Start Sprite"] = "Crear sprite de inicio de trade",
        ["Create User List"] = "Crear lista de usuarios",
        ["Create Users In Queue"] = "Crear usuarios en cola",
        ["Create Waited Time"] = "Crear tiempo esperado",
        ["Estimated Fulfillment Format"] = "Formato de cumplimiento estimado",
        ["Estimated Time Format"] = "Formato de tiempo estimado",
        ["On Deck Format"] = "Formato de On Deck",
        ["On Deck Format2"] = "Formato de On Deck 2",
        ["On Deck Separator"] = "Separador de On Deck",
        ["On Deck Separator2"] = "Separador de On Deck 2",
        ["On Deck Skip"] = "Omitir en On Deck",
        ["On Deck Skip2"] = "Omitir en On Deck 2",
        ["On Deck Take"] = "Cantidad de On Deck",
        ["On Deck Take2"] = "Cantidad de On Deck 2",
        ["Trade Block File"] = "Archivo de bloque de trade",
        ["Trade Block Format"] = "Formato de bloque de trade",
        ["Trainer Trade Start"] = "Inicio de trade del entrenador",
        ["User List Format"] = "Formato de lista de usuarios",
        ["User List Separator"] = "Separador de lista de usuarios",
        ["User List Skip"] = "Omitir en lista de usuarios",
        ["User List Take"] = "Cantidad de lista de usuarios",
        ["Users In Queue Format"] = "Formato de usuarios en cola",
        ["Waited Time Format"] = "Formato de tiempo esperado",

        ["Avoid System Update"] = "Evitar actualización del sistema",
        ["Check Game Delay"] = "Comprobar retraso del juego",
        ["Closing Game Settings"] = "Ajustes de cierre del juego",
        ["Extra Reconnect Delay"] = "Retraso extra de reconexión",
        ["Extra Time Add Friend"] = "Tiempo extra para agregar amigo",
        ["Extra Time Check Game"] = "Tiempo extra para comprobar juego",
        ["Extra Time Close Game"] = "Tiempo extra para cerrar juego",
        ["Extra Time Connect Online"] = "Tiempo extra para conectar online",
        ["Extra Time Delete Friend"] = "Tiempo extra para eliminar amigo",
        ["Extra Time End Raid"] = "Tiempo extra para terminar raid",
        ["Extra Time Join Union Room"] = "Tiempo extra para entrar a sala Unión",
        ["Extra Time Leave Union Room"] = "Tiempo extra para salir de sala Unión",
        ["Extra Time Load Game"] = "Tiempo extra para cargar juego",
        ["Extra Time Load Overworld"] = "Tiempo extra para cargar overworld",
        ["Extra Time Load Portal"] = "Tiempo extra para cargar portal",
        ["Extra Time Load Profile"] = "Tiempo extra para cargar perfil",
        ["Extra Time Load Raid"] = "Tiempo extra para cargar raid",
        ["Extra Time Open Box"] = "Tiempo extra para abrir caja",
        ["Extra Time Open Code Entry"] = "Tiempo extra para abrir entrada de código",
        ["Extra Time Open Raid"] = "Tiempo extra para abrir raid",
        ["Extra Time Open Y Menu"] = "Tiempo extra para abrir menú Y",
        ["Extra Time Return Home"] = "Tiempo extra para volver a HOME",
        ["Keypress Time"] = "Tiempo de pulsación",
        ["Miscellaneous Settings"] = "Ajustes varios",
        ["Opening Game Settings"] = "Ajustes de apertura del juego",
        ["Profile Selection Required"] = "Selección de perfil requerida",
        ["Raid Settings"] = "Ajustes de raid",
        ["Reconnect Attempts"] = "Intentos de reconexión",

        ["Ban ID When Blocking User"] = "Banear ID al bloquear usuario",
        ["Banned ID Match Echo Mention"] = "Mención al detectar ID baneado",
        ["Banned IDs"] = "IDs baneados",
        ["Block Detected Banned User"] = "Bloquear usuario baneado detectado",
        ["Cooldown Abuse Echo Mention"] = "Mención por abuso de cooldown",
        ["Echo Nintendo Online ID Cooldown"] = "Mostrar Nintendo Online ID en cooldown",
        ["Echo Nintendo Online ID Ledy"] = "Mostrar Nintendo Online ID en Ledy",
        ["Echo Nintendo Online ID Multi"] = "Mostrar Nintendo Online ID en multi",
        ["Echo Nintendo Online ID Multi Recipients"] = "Mostrar Nintendo Online ID en destinatarios multi",
        ["Ledy Abuse Echo Mention"] = "Mención por abuso de Ledy",
        ["Multi Abuse Echo Mention"] = "Mención por abuso multi",
        ["Multi Recipient Echo Mention"] = "Mención por destinatarios multi",
        ["Trade Abuse Action"] = "Acción por abuso de trade",
        ["Trade Abuse Expiration"] = "Expiración de abuso de trade",
        ["Trade Cooldown"] = "Cooldown de trade",

        ["Min Trade Code"] = "Código mínimo de trade",
        ["Max Trade Code"] = "Código máximo de trade",
        ["Store Trade Codes"] = "Guardar códigos de trade",
        ["Trade Wait Time"] = "Tiempo de espera del trade",
        ["Max Trade Confirm Time"] = "Tiempo máximo para confirmar trade",
        ["Item Trade Species"] = "Especie para Item Trade",
        ["Default Held Item"] = "Item predeterminado",
        ["Suggest Relearn Moves"] = "Sugerir movimientos reaprendibles",
        ["Allow Batch Trades"] = "Permitir trades por lotes",
        ["Enable Spam Check"] = "Habilitar verificación de spam",
        ["Max Pkms Per Trade"] = "Máximo de Pokémon por trade",
        ["Max Dumps Per Trade"] = "Máximo de dumps por trade",
        ["Max Dump Trade Time"] = "Tiempo máximo para dump trade",
        ["Dump Trade Legality Check"] = "Comprobar legalidad en dump trade",
        ["Disallow Trade Evolve"] = "Bloquear evolución por trade",
        ["Trade Animation Max Delay Seconds"] = "Retraso máximo de animación de trade",
        ["Preferred Image Size"] = "Tamaño preferido de imagen",
        ["Extra Embed Options"] = "Opciones extra del embed",
        ["Use Embeds"] = "Usar embeds",
        ["Trading Bot URL"] = "URL del bot de trade",
        ["Trading Bot Url"] = "URL del bot de trade",
        ["Non Native Tex T"] = "Texto para Pokémon no nativo",
        ["Move Type Emojis"] = "Emojis de tipo de movimiento",
        ["Use Tera Emojis"] = "Usar emojis de tipo Tera",
        ["Use Scale Emojis"] = "Usar emojis de tamaño",
        ["Custom Type Emojis"] = "Emojis de tipos personalizados",
        ["Tera Type Emojis"] = "Emojis de tipo Tera",
        ["Use Plus Move Emoji"] = "Emoji de movimiento Plus",
        ["Scale Emojis"] = "Emojis de tamaño",
        ["Scale XXXS Emoji"] = "Emoji de escala XXXS",
        ["Scale XXXL Emoji"] = "Emoji de escala XXXL",
        ["Shiny Emojis"] = "Emojis shiny",
        ["Shiny Square Emoji"] = "Emoji shiny cuadrado",
        ["Shiny Normal Emoji"] = "Emoji shiny normal",
        ["Gender Emojis"] = "Emojis de género",
        ["Male Emoji"] = "Emoji masculino",
        ["Female Emoji"] = "Emoji femenino",
        ["Special Marks Emojis"] = "Emojis de marcas especiales",
        ["Mystery Gift Emoji"] = "Emoji de regalo misterioso",
        ["Alpha Mark Emoji"] = "Emoji de marca alfa",
        ["Mightiest Mark Emoji"] = "Emoji de marca imbatible",
        ["Alpha PLA Emoji"] = "Emoji alfa PLA",
        ["Gigantamax Emoji"] = "Emoji Gigamax",
        ["Show Scale"] = "Mostrar tamaño",
        ["Show Tera Type"] = "Mostrar tipo Tera",
        ["Show Level"] = "Mostrar nivel",
        ["Show Ball"] = "Mostrar Ball",
        ["Show Met Level"] = "Mostrar nivel de encuentro",
        ["Show Met Date"] = "Mostrar fecha de encuentro",
        ["Show Met Location"] = "Mostrar ubicación de encuentro",
        ["Show Ability"] = "Mostrar habilidad",
        ["Show Nature"] = "Mostrar naturaleza",
        ["Show Language"] = "Mostrar idioma",
        ["Show IVs"] = "Mostrar IVs",
        ["Show EVs"] = "Mostrar EVs",
        ["Show GVs"] = "Mostrar GVs",
        ["Show AVs"] = "Mostrar AVs",
        ["Show Tracker"] = "Mostrar tracker",
        ["Allow Requests"] = "Permitir solicitudes",
        ["GID"] = "GID",
        ["Events Folder"] = "Carpeta de Eventos",
        ["Battle-Ready Folder"] = "Carpeta Battle-Ready",
        ["Battle Ready PKM Folder"] = "Carpeta PKM Battle Ready",
        ["Screen Off"] = "Apagar pantalla",
        ["Emit Counts On Status Check"] = "Emitir conteos al consultar estado",
        ["Move Type"] = "Tipo de movimiento",
        ["Emoji Code"] = "Código del emoji",
        ["Emoji String"] = "Texto del emoji",
        ["Trade Configuration"] = "Configuración del trade",
        ["Trade Embed Settings"] = "Ajustes del embed de trade",
        ["Content"] = "Contenido",
        ["Displayed Content"] = "Contenido mostrado",
        ["General Embed Settings"] = "Ajustes generales del embed",
        ["Embed Emoji Settings"] = "Ajustes de emojis del embed",
        ["Displayed Embed Fields"] = "Campos visibles del embed",
        ["Count Stats Settings"] = "Ajustes de estadísticas de conteo",
        ["Completed Surprise"] = "Trades sorpresa completados",
        ["Completed Distribution"] = "Trades de distribución completados",
        ["Completed Trades"] = "Trades completados",
        ["Completed Fix O Ts"] = "Trades FixOT completados",
        ["Completed Seed Checks"] = "Comprobaciones de semilla completadas",
        ["Completed Clones"] = "Clonaciones completadas",
        ["Completed Dumps"] = "Dumps completados",
        ["VGCPastes Configuration"] = "Configuración de VGCPastes",
    };

    private static readonly Dictionary<string, string> EnglishToSpanishSettingsText = BuildEnglishToSpanishSettingsText();

    private static readonly (string Source, string Replacement)[] SpanishSettingsPhraseFallbacks =
    [
        ("Pokémon", "Pokemon"),
        ("pokémon", "Pokemon"),
        ("Configuración", "Settings"),
        ("configuración", "settings"),
        ("Ajustes", "Settings"),
        ("ajustes", "settings"),
        ("Canales", "Channels"),
        ("canales", "channels"),
        ("cola", "queue"),
        ("Cola", "Queue"),
        ("intercambio", "trade"),
        ("Intercambio", "Trade"),
        ("intercambios", "trades"),
        ("Intercambios", "Trades"),
        ("trade", "trade"),
        ("trades", "trades"),
        ("usuario", "user"),
        ("usuarios", "users"),
        ("Usuario", "User"),
        ("Usuarios", "Users"),
        ("mensaje", "message"),
        ("mensajes", "messages"),
        ("Mensaje", "Message"),
        ("Mensajes", "Messages"),
        ("carpeta", "folder"),
        ("carpetas", "folders"),
        ("Carpeta", "Folder"),
        ("Carpetas", "Folders"),
        ("archivo", "file"),
        ("archivos", "files"),
        ("Archivo", "File"),
        ("Archivos", "Files"),
        ("código", "code"),
        ("Código", "Code"),
        ("tiempo", "time"),
        ("Tiempo", "Time"),
        ("segundos", "seconds"),
        ("habilitado", "enabled"),
        ("habilitada", "enabled"),
        ("deshabilitado", "disabled"),
        ("deshabilitada", "disabled"),
        ("predeterminado", "default"),
        ("predeterminada", "default"),
        ("notificación", "notification"),
        ("notificaciones", "notifications"),
        ("Notificación", "Notification"),
        ("Notificaciones", "Notifications"),
        ("operación", "operation"),
        ("operaciones", "operations"),
        ("Operación", "Operation"),
        ("Operaciones", "Operations"),
        ("comercial", "trade"),
        ("comerciales", "trade"),
        ("enlace", "link"),
        ("Enlace", "Link"),
        ("conteo", "count"),
        ("Conteo", "Count"),
        ("recuento", "count"),
        ("Recuento", "Count"),
        ("disponibles", "available"),
        ("actualmente", "currently"),
        ("espera", "wait"),
        ("Esperar", "Wait"),
    ];

    private static readonly Dictionary<string, string> SpanishToEnglishSettingsWordFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["activa"] = "enables",
        ["activar"] = "enable",
        ["activos"] = "assets",
        ["agregar"] = "add",
        ["ajustes"] = "settings",
        ["aplica"] = "applies",
        ["archivo"] = "file",
        ["archivos"] = "files",
        ["barra"] = "bar",
        ["bloqueo"] = "block",
        ["bot"] = "bot",
        ["canal"] = "channel",
        ["canales"] = "channels",
        ["carpeta"] = "folder",
        ["carpetas"] = "folders",
        ["cerrar"] = "close",
        ["código"] = "code",
        ["comando"] = "command",
        ["comandos"] = "commands",
        ["comercial"] = "trade",
        ["comerciales"] = "trade",
        ["comercio"] = "trade",
        ["configuración"] = "settings",
        ["configura"] = "set",
        ["conexión"] = "connection",
        ["conservar"] = "keep",
        ["conteo"] = "count",
        ["control"] = "control",
        ["crear"] = "create",
        ["desactivar"] = "disable",
        ["deshabilitado"] = "disabled",
        ["deshabilitar"] = "disable",
        ["destino"] = "destination",
        ["directo"] = "live",
        ["disponibles"] = "available",
        ["donación"] = "donation",
        ["donaciones"] = "donations",
        ["eliminar"] = "delete",
        ["eliminación"] = "deletion",
        ["enlace"] = "link",
        ["envía"] = "sends",
        ["enviar"] = "send",
        ["espera"] = "wait",
        ["esperar"] = "wait",
        ["estado"] = "status",
        ["estimada"] = "estimated",
        ["estimado"] = "estimated",
        ["función"] = "role",
        ["generar"] = "generate",
        ["habilitado"] = "enabled",
        ["habilitar"] = "enable",
        ["icono"] = "icon",
        ["iconos"] = "icons",
        ["imagen"] = "image",
        ["incursión"] = "raid",
        ["información"] = "information",
        ["inicio"] = "start",
        ["lista"] = "list",
        ["Lista"] = "List",
        ["log"] = "log",
        ["marca"] = "mark",
        ["mensajes"] = "messages",
        ["mensaje"] = "message",
        ["miniatura"] = "thumbnail",
        ["mostrar"] = "show",
        ["número"] = "number",
        ["operación"] = "operation",
        ["operaciones"] = "operations",
        ["permitir"] = "allow",
        ["predeterminado"] = "default",
        ["progreso"] = "progress",
        ["recuperación"] = "recovery",
        ["recuento"] = "count",
        ["registro"] = "log",
        ["registros"] = "logs",
        ["respuesta"] = "reply",
        ["segundos"] = "seconds",
        ["servidor"] = "server",
        ["solicitud"] = "request",
        ["solicitudes"] = "requests",
        ["tiempo"] = "time",
        ["trade"] = "trade",
        ["trades"] = "trades",
        ["usuario"] = "user",
        ["usuarios"] = "users",
        ["valor"] = "value",
    };

    private static readonly Dictionary<string, string> SpanishPhraseFallbacks = new(StringComparer.Ordinal)
    {
        ["PKM"] = "PKM",
        ["HOME"] = "HOME",
        ["OT"] = "OT",
        ["TID"] = "TID",
        ["SID"] = "SID",
        ["ID"] = "ID",
        ["IVs"] = "IVs",
        ["EVs"] = "EVs",
        ["GVs"] = "GVs",
        ["AVs"] = "AVs",
        ["LGPE"] = "LGPE",
        ["BDSP"] = "BDSP",
        ["SWSH"] = "SWSH",
        ["PLA"] = "PLA",
        ["PLZA"] = "PLZA",
        ["URL"] = "URL",
        ["DMs"] = "DMs",
        ["XP"] = "XP",
    };

    private static readonly Dictionary<string, string> EnglishToSpanishWordFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Abuse"] = "abuso",
        ["Action"] = "acción",
        ["Add"] = "agregar",
        ["Additional"] = "adicional",
        ["Allow"] = "permitir",
        ["Allowed"] = "permitido",
        ["Alpha"] = "alfa",
        ["Any"] = "cualquier",
        ["Attempt"] = "intento",
        ["Attempts"] = "intentos",
        ["Auto"] = "automático",
        ["Avoid"] = "evitar",
        ["Backoff"] = "retroceso",
        ["Badge"] = "insignia",
        ["Badges"] = "insignias",
        ["Ball"] = "Ball",
        ["Ban"] = "banear",
        ["Banned"] = "baneados",
        ["Barrier"] = "barrera",
        ["Batch"] = "lote",
        ["Battle"] = "batalla",
        ["Blacklist"] = "lista negra",
        ["Block"] = "bloquear",
        ["Bot"] = "bot",
        ["Can"] = "puede",
        ["Canceled"] = "cancelado",
        ["Capture"] = "capturar",
        ["Channel"] = "canal",
        ["Channels"] = "canales",
        ["Check"] = "comprobar",
        ["Clone"] = "clonar",
        ["Close"] = "cerrar",
        ["Code"] = "código",
        ["Color"] = "color",
        ["Command"] = "comando",
        ["Commands"] = "comandos",
        ["Completed"] = "completados",
        ["Config"] = "configuración",
        ["Connect"] = "conectar",
        ["Connections"] = "conexiones",
        ["Control"] = "control",
        ["Cooldown"] = "cooldown",
        ["Copy"] = "copiar",
        ["Count"] = "cantidad",
        ["Counts"] = "conteos",
        ["Create"] = "crear",
        ["Current"] = "actual",
        ["Custom"] = "personalizado",
        ["Delay"] = "retraso",
        ["Delete"] = "eliminar",
        ["Detected"] = "detectado",
        ["Directory"] = "directorio",
        ["Discord"] = "Discord",
        ["Distribute"] = "distribuir",
        ["Distribution"] = "distribución",
        ["Donation"] = "donación",
        ["Dump"] = "dump",
        ["Echo"] = "eco",
        ["Embed"] = "embed",
        ["Emoji"] = "emoji",
        ["Emojis"] = "emojis",
        ["Enable"] = "habilitar",
        ["Enabled"] = "habilitado",
        ["Encounter"] = "encuentro",
        ["End"] = "terminar",
        ["Error"] = "error",
        ["Estimated"] = "estimado",
        ["Events"] = "eventos",
        ["Expiration"] = "expiración",
        ["External"] = "externas",
        ["Extra"] = "extra",
        ["Favored"] = "favorecido",
        ["File"] = "archivo",
        ["Folder"] = "carpeta",
        ["Folders"] = "carpetas",
        ["Force"] = "forzar",
        ["Format"] = "formato",
        ["Friend"] = "amigo",
        ["Game"] = "juego",
        ["Gender"] = "género",
        ["Global"] = "global",
        ["Goal"] = "meta",
        ["Hello"] = "saludo",
        ["Hidden"] = "oculto",
        ["History"] = "historial",
        ["Icon"] = "icono",
        ["Icons"] = "iconos",
        ["Idle"] = "inactivo",
        ["Image"] = "imagen",
        ["Initial"] = "inicial",
        ["Intentional"] = "intencionales",
        ["Interval"] = "intervalo",
        ["Join"] = "unirse",
        ["Keyboard"] = "teclado",
        ["Keypress"] = "pulsación",
        ["Language"] = "idioma",
        ["Leave"] = "salir",
        ["Ledy"] = "Ledy",
        ["Legal"] = "legales",
        ["Legality"] = "legalidad",
        ["Level"] = "nivel",
        ["Link"] = "link",
        ["List"] = "lista",
        ["Load"] = "cargar",
        ["Log"] = "log",
        ["Logging"] = "registros",
        ["Match"] = "coincidencia",
        ["Matching"] = "coincidentes",
        ["Max"] = "máximo",
        ["Message"] = "mensaje",
        ["Messages"] = "mensajes",
        ["Met"] = "encuentro",
        ["Min"] = "mínimo",
        ["Mode"] = "modo",
        ["Module"] = "módulo",
        ["Multi"] = "multi",
        ["Name"] = "nombre",
        ["Native"] = "nativo",
        ["Natives"] = "nativos",
        ["Nature"] = "naturaleza",
        ["Nintendo"] = "Nintendo",
        ["Notify"] = "notificar",
        ["Offline"] = "offline",
        ["Online"] = "online",
        ["Open"] = "abrir",
        ["Option"] = "opción",
        ["Options"] = "opciones",
        ["Order"] = "orden",
        ["Overworld"] = "overworld",
        ["Partner"] = "compañero",
        ["Path"] = "ruta",
        ["Platform"] = "plataforma",
        ["Portal"] = "portal",
        ["Prefix"] = "prefijo",
        ["Priority"] = "prioridad",
        ["Profile"] = "perfil",
        ["Progress"] = "progreso",
        ["Queue"] = "cola",
        ["Queues"] = "colas",
        ["Raid"] = "raid",
        ["Random"] = "aleatorio",
        ["Ready"] = "listo",
        ["Reconnect"] = "reconexión",
        ["Recovery"] = "recuperación",
        ["Remote"] = "remoto",
        ["Reply"] = "respuesta",
        ["Request"] = "solicitud",
        ["Requests"] = "solicitudes",
        ["Required"] = "requerido",
        ["Reset"] = "reiniciar",
        ["Result"] = "resultado",
        ["Results"] = "resultados",
        ["Return"] = "devolver",
        ["Ribbons"] = "cintas",
        ["Role"] = "rol",
        ["Roles"] = "roles",
        ["Room"] = "sala",
        ["Scale"] = "escala",
        ["Search"] = "buscar",
        ["Seconds"] = "segundos",
        ["Secret"] = "secreto",
        ["Seed"] = "semilla",
        ["Selection"] = "selección",
        ["Separator"] = "separador",
        ["Server"] = "servidor",
        ["Set"] = "set",
        ["Settings"] = "ajustes",
        ["Shiny"] = "shiny",
        ["Show"] = "mostrar",
        ["Shuffled"] = "aleatorio",
        ["Skip"] = "omitir",
        ["Sparkle"] = "brillo",
        ["Species"] = "especie",
        ["Specified"] = "especificada",
        ["Sprite"] = "sprite",
        ["Stable"] = "estable",
        ["Start"] = "inicio",
        ["Starting"] = "inicio",
        ["Status"] = "estado",
        ["Stop"] = "parada",
        ["Stream"] = "stream",
        ["Sudo"] = "sudo",
        ["Synchronize"] = "sincronizar",
        ["System"] = "sistema",
        ["Take"] = "tomar",
        ["Target"] = "objetivo",
        ["Tera"] = "Tera",
        ["Thanks"] = "gracias",
        ["Theme"] = "tema",
        ["Threshold"] = "umbral",
        ["Throttle"] = "límite",
        ["Time"] = "tiempo",
        ["Timeout"] = "tiempo máximo",
        ["Timings"] = "tiempos",
        ["Token"] = "token",
        ["Trade"] = "trade",
        ["Trades"] = "trades",
        ["Tracked"] = "rastreados",
        ["Tracker"] = "tracker",
        ["Trainer"] = "entrenador",
        ["Tutorial"] = "tutorial",
        ["Type"] = "tipo",
        ["Union"] = "Unión",
        ["Unwanted"] = "no deseadas",
        ["Update"] = "actualización",
        ["Uptime"] = "tiempo activo",
        ["Use"] = "usar",
        ["User"] = "usuario",
        ["Users"] = "usuarios",
        ["Version"] = "versión",
        ["Video"] = "video",
        ["Wait"] = "espera",
        ["Waited"] = "esperado",
        ["Waiting"] = "espera",
        ["Web"] = "web",
        ["Whitelist"] = "lista blanca",
        ["Whisper"] = "susurro",
        ["Whispers"] = "susurros",
        ["Window"] = "ventana",
        ["Yield"] = "peso",
    };

    private static Dictionary<string, string> BuildEnglishToSpanishSettingsText()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Bot Trade"] = "Bot Trade",
            ["Bot Encounter"] = "Bot Encounter",
            ["Bot"] = "Bot",
            ["Integration"] = "Integration",
            ["Operation"] = "Operation",
            ["Feature Toggle"] = "Feature Toggle",
            ["Debug"] = "Depuración",
            ["Channels"] = "Canales",
            ["Roles"] = "Roles",
            ["Moderation"] = "Moderacion",
            ["Moderation Settings"] = "Ajustes de moderacion",
            ["Servers"] = "Servidores",
            ["Startup"] = "Inicio",
            ["Users"] = "Usuarios",
            ["Appearance"] = "Apariencia",
            ["Generate"] = "Generación",
            ["Misc"] = "Varios",
            ["Queue Toggle"] = "Control de cola",
            ["Time Bias"] = "Prioridad por tiempo",
            ["User Bias"] = "Prioridad por usuarios",
            ["Distribute"] = "Distribución",
            ["Synchronize"] = "Sincronización",
            ["Trade Config"] = "Configuración de trade",
            ["Embed Settings"] = "Configuración del embed",
            ["Path to your Events Folder. Create a new folder called 'events' and copy the path here."] = "Ruta a tu carpeta de eventos. Crea una carpeta nueva llamada 'events' y copia la ruta aquí.",
            ["Path to your BattleReady Folder. Create a new folder called 'battleready' and copy the path here."] = "Ruta a tu carpeta BattleReady. Crea una carpeta nueva llamada 'battleready' y copia la ruta aquí.",
            ["Count Stats"] = "Estadísticas de conteo",
            ["VGCPastes Config"] = "Configuración de VGCPastes",
            ["Discord OAuth Client ID"] = "ID de cliente OAuth de Discord",
            ["Discord OAuth Client Secret"] = "Secreto de cliente OAuth de Discord",
            ["Discord OAuth Redirect URI"] = "URI de redireccion OAuth de Discord",
            ["Admin ID"] = "ID de administrador",
            ["AdminID"] = "ID de administrador",
            ["Anyone allowed"] = "Cualquiera permitido",
            ["None allowed (none specified)."] = "Ninguno permitido (ninguno especificado).",
            ["Use Keyboard"] = "Usar teclado",
            ["Discord"] = "Discord",
            ["Token"] = "Token",
            ["Additional Embed Text"] = "Texto adicional del embed",
            ["Allow Global Sudo"] = "Permitir sudo global",
            ["Announcement Channels"] = "Canales de anuncios",
            ["Abuse Log Channels"] = "Canales de registro de abusos",
            ["Announcement Settings"] = "Ajustes de los anuncios",
            ["Bot Color Status Trade Only"] = "Color de estado solo para bots de trade",
            ["Bot Embed Status"] = "Embed de estado del bot",
            ["Channel Status Config"] = "Configuración de estado del canal",
            ["Bot Game Status"] = "Estado personalizado del bot",
            ["Bot Custom Status"] = "Estado personalizado del bot",
            ["Enable XP System"] = "Habilitar sistema de XP",
            ["Message Icons"] = "Iconos de mensajes",
            ["Custom Badge Emojis"] = "Emojis personalizados de insignias",
            ["Channel Status"] = "Estado del canal",
            ["Channel Whitelist"] = "Lista blanca de canales",
            ["Command Prefix"] = "Prefijo de comando",
            ["Allow Any Prefix"] = "Permitir cualquier prefijo",
            ["Convert PKM Reply Any Channel"] = "Responder PKM en cualquier canal",
            ["Convert PKM To Showdown Set"] = "Convertir PKM a Showdown Set",
            ["UserDMsToBotForwarder"] = "Reenviar DMs del usuario al bot",
            ["Forward User DMs To Bot"] = "Reenviar DMs del usuario al bot",
            ["User DMs To Bot Forwarder"] = "Reenviar DMs del usuario al bot",
            ["User D Ms To Bot Forwarder"] = "Reenviar DMs del usuario al bot",
            ["Global Sudo List"] = "Lista de sudos globales",
            ["Hello Response"] = "Respuesta de saludo",
            ["Logging Channels"] = "Canales de registro",
            ["Stream"] = "Stream",
            ["Donation"] = "Donación",
            ["Module Blacklist"] = "Lista negra de módulos",
            ["Offline Emoji"] = "Emoji offline",
            ["Online Emoji"] = "Emoji online",
            ["ReplyCannotUseCommandInChannel"] = "Responder cuando el comando no está permitido",
            ["Reply Cannot Use Command In Channel"] = "Responder cuando el comando no está permitido",
            ["Reply When Commands Are Not Allowed"] = "Responder cuando el comando no está permitido",
            ["Reply To Thanks"] = "Responder agradecimientos",
            ["Return PKMs"] = "Devolver PKM",
            ["Message Deletion Enabled"] = "Eliminación de mensajes activada",
            ["Error Message Delete Delay Seconds"] = "Retraso para eliminar mensajes de error",
            ["Delete User Command Messages"] = "Eliminar comandos de usuario",
            ["Role Can Clone"] = "Rol para clonar",
            ["Role Can Dump"] = "Rol para dump",
            ["Role Can Fix OT"] = "Rol para Fix OT",
            ["RoleCanSeedCheckorSpecialRequest"] = "Rol para Seed Check o solicitud especial",
            ["Role Can Seed Checkor Special Request"] = "Rol para Seed Check o solicitud especial",
            ["Role Can Seed Check or Special Request"] = "Rol para Seed Check o solicitud especial",
            ["Role Can Trade"] = "Rol para trade",
            ["Role Can Trade Plus"] = "Rol para Trade Plus",
            ["Role Favored"] = "Rol favorecido",
            ["Role Remote Control"] = "Rol de control remoto",
            ["Role Sudo"] = "Rol sudo",
            ["Server Blacklist"] = "Lista negra de servidores",
            ["Trade Starting Channels"] = "Canales de inicio de trade",
            ["Full Trade Error Log Channels"] = "Canales de log completo de errores de trade",
            ["User Blacklist"] = "Lista negra de usuarios",
            ["Warning"] = "Aviso",
            ["Success"] = "Éxito",
            ["Error"] = "Error",
            ["Direct Message"] = "Mensaje directo",
            ["Waiting"] = "Espera",
            ["AllowIfEmpty"] = "Permitir si está vacío",
            ["Allow If Empty"] = "Permitir si está vacío",
            ["List"] = "Lista",
            ["Enable Channel Status"] = "Activar estado del canal",
            ["Stream Link"] = "Link al stream",
            ["Stream Icon"] = "Icono de stream",
            ["Donation Link"] = "Link de donación",
            ["Progress Bar"] = "Barra de progreso",
            ["Show Progress Bar"] = "Mostrar barra de progreso",
            ["Donation Goal"] = "Meta de donaciones",
            ["Current Donations"] = "Donaciones actuales",
            ["Announcement Embed Color"] = "Color del embed de anuncios",
            ["Announcement Thumbnail Option"] = "Miniatura de anuncios",
            ["Custom Announcement Thumbnail URL"] = "URL personalizada de miniatura de anuncios",
            ["Random Announcement Color"] = "Color aleatorio de anuncios",
            ["Random Announcement Thumbnail"] = "Miniatura aleatoria de anuncios",
            ["Bot Name"] = "Nombre del bot",
            ["Bot Logo Image"] = "Imagen del logo del bot",
            ["Distribution"] = "Distribución",
            ["Favoritism"] = "Favoritismo",
            ["Queues"] = "Colas",
            ["Stop Conditions"] = "Condiciones de parada",
            ["Timings"] = "Tiempos",
            ["Trade"] = "Trade",
            ["Trade Abuse"] = "Abuso de trade",
            ["Twitch"] = "Twitch",
            ["YouTube"] = "YouTube",
            ["You Tube"] = "YouTube",
            ["Recovery"] = "Recuperación",
            ["Web Server"] = "Servidor web",
            ["SysBot.Pokemon.WebServerSettings"] = "Configuración del servidor web",
            ["Folder"] = "Carpetas",
            ["Legality"] = "Legalidad",
        };

        foreach (var (spanish, english) in SettingsText)
            result.TryAdd(english, spanish);

        foreach (var (english, spanish) in CuratedSpanishSettingNames)
            result[english] = spanish;

        return result;
    }

    private sealed class LocalizedTypeDescriptionProvider(TypeDescriptionProvider parent) : TypeDescriptionProvider(parent)
    {
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
        {
            return new LocalizedTypeDescriptor(base.GetTypeDescriptor(objectType, instance)!);
        }
    }

    private sealed class LocalizedTypeDescriptor(ICustomTypeDescriptor parent) : CustomTypeDescriptor(parent)
    {
        public override PropertyDescriptorCollection GetProperties() => Localize(base.GetProperties());

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => Localize(base.GetProperties(attributes));

        private static PropertyDescriptorCollection Localize(PropertyDescriptorCollection properties)
        {
            var localized = properties.Cast<PropertyDescriptor>()
                .Select(p => new LocalizedPropertyDescriptor(p))
                .ToArray<PropertyDescriptor>();

            return new PropertyDescriptorCollection(localized, true);
        }
    }

    private sealed class LocalizedPropertyDescriptor(PropertyDescriptor parent) : PropertyDescriptor(parent)
    {
        public override string DisplayName => LocalizeDisplayName(parent.DisplayName, parent.Name);

        public override string Description => LocalizeDescription(parent.Description);

        public override string Category => LocalizeCategory(parent.Category);

        public override TypeConverter Converter => new LocalizedValueTypeConverter(parent.Converter, parent.Name);

        public override bool CanResetValue(object component) => parent.CanResetValue(component);

        public override Type ComponentType => parent.ComponentType;

        public override object? GetValue(object? component) => parent.GetValue(component);

        public override bool IsReadOnly => parent.IsReadOnly;

        public override Type PropertyType => parent.PropertyType;

        public override void ResetValue(object component) => parent.ResetValue(component);

        public override void SetValue(object? component, object? value) => parent.SetValue(component, value);

        public override bool ShouldSerializeValue(object component) => parent.ShouldSerializeValue(component);
    }

    private sealed class LocalizedValueTypeConverter(TypeConverter parent, string propertyName) : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
            parent.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
            parent.CanConvertTo(context, destinationType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
            parent.ConvertFrom(context, culture, value);

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            var converted = parent.ConvertTo(context, culture, value, destinationType);
            return destinationType == typeof(string) && converted is string text
                ? LocalizeValueText(text, propertyName)
                : converted;
        }

        public override object? CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues) =>
            parent.CreateInstance(context, propertyValues)!;

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) =>
            parent.GetCreateInstanceSupported(context);

        public override PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes) =>
            parent.GetProperties(context, value, attributes);

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) =>
            parent.GetPropertiesSupported(context);

        public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context) =>
            parent.GetStandardValues(context);

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) =>
            parent.GetStandardValuesExclusive(context);

        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) =>
            parent.GetStandardValuesSupported(context);

        public override bool IsValid(ITypeDescriptorContext? context, object? value) =>
            parent.IsValid(context, value);
    }
}
