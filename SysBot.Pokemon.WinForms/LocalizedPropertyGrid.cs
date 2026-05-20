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

        return new string(chars.ToArray());
    }

    private static readonly Dictionary<string, string> SettingsText = new(StringComparer.Ordinal)
    {
        ["Ajustes de configuración de Trade"] = "Trade Configuration Settings",
        ["Ajustes de configuración de trade"] = "Trade Configuration Settings",
        ["Ajustes de configuración de Trade Embed"] = "Trade Embed Settings",
        ["Configuración del Embed Trade"] = "Trade Embed Settings",
        ["Configuración del trade"] = "Trade Configuration",
        ["Configuración de las carpetas de solicitud."] = "Request Folder Settings",
        ["Configuración de las carpetas de solicitud"] = "Request Folder Settings",
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
        ["Integration"] = "Integration",
        ["Operation"] = "Operation",
        ["FeatureToggle"] = "Feature Toggle",
        ["Debug"] = "Debug",
        ["Channels"] = "Channels",
        ["Roles"] = "Roles",
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
        ["RequestFolders"] = "Request Folders",
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
        ["Ajustes de los Anuncios"] = "Announcement Settings",
        ["Estado de Juego del Bot"] = "Bot Game Status",
        ["Sistema de XP"] = "XP System",
        ["Iconos de mensajes"] = "Message Icons",
        ["Insignias"] = "Badges",
        ["Estado del canal"] = "Channel Status",
        ["Permitir cualquier prefijo"] = "Allow Any Prefix",
        ["Reenviar DMs"] = "Forward DMs",
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
        ["Custom Badge Emojis"] = "Custom Badge Emojis",
        ["Channel Status"] = "Channel Status",
        ["Channel Whitelist"] = "Channel Whitelist",
        ["Command Prefix"] = "Command Prefix",
        ["Allow Any Prefix"] = "Allow Any Prefix",
        ["Convert PKM Reply Any Channel"] = "Convert PKM Reply Any Channel",
        ["Convert PKM To Showdown Set"] = "Convert PKM To Showdown Set",
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
        ["Bot Logo Sparkle Color1"] = "Bot Logo Sparkle Color 1",
        ["Bot Logo Sparkle Color2"] = "Bot Logo Sparkle Color 2",
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
        ["Deshabilitar esto eliminará la compatibilidad global con sudo."] = "Disabling this removes global sudo support.",
        ["Emojis que se usan en mensajes visibles de Discord. Si un valor se deja vacio, se usa el icono predeterminado."] = "Emojis used in visible Discord messages. If a value is empty, the default icon is used.",
        ["Habilita o deshabilita el sistema de XP para usuarios cuando usan comandos."] = "Enables or disables the XP system for users when they use commands.",
        ["Enviará un estado embed para cuando el bot este online/offline a todos los canales incluidos en la lista blanca."] = "Sends an online/offline status embed to every whitelisted channel.",
        ["Estado personalizado del bot."] = "Custom bot status.",
        ["Lista de emojis personalizados para insignias que se entregan al usuario luego de completar cierta cantidad de trades."] = "List of custom badge emojis awarded after users complete certain trade counts.",
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
        ["Ajustes relacionados con las carpetas de solicitud."] = "Settings related to request folders.",
        ["Ajustes relacionados con las estadísticas de recuento de trades."] = "Settings related to trade count statistics.",
        ["Ajustes relacionados con la Configuración de VGCPastes."] = "Settings related to VGCPastes configuration.",
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
            ["Integration"] = "Integration",
            ["Operation"] = "Operation",
            ["Feature Toggle"] = "Feature Toggle",
            ["Debug"] = "Depuración",
            ["Channels"] = "Canales",
            ["Roles"] = "Roles",
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
            ["Request Folders"] = "Carpetas de solicitud",
            ["Count Stats"] = "Estadísticas de conteo",
            ["VGCPastes Config"] = "Configuración de VGCPastes",
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
            ["Bot Game Status"] = "Estado de juego del bot",
            ["Enable XP System"] = "Habilitar sistema de XP",
            ["Message Icons"] = "Iconos de mensajes",
            ["Custom Badge Emojis"] = "Emojis personalizados de insignias",
            ["Channel Status"] = "Estado del canal",
            ["Channel Whitelist"] = "Lista blanca de canales",
            ["Command Prefix"] = "Prefijo de comando",
            ["Allow Any Prefix"] = "Permitir cualquier prefijo",
            ["Convert PKM Reply Any Channel"] = "Responder PKM en cualquier canal",
            ["Convert PKM To Showdown Set"] = "Convertir PKM a Showdown Set",
            ["Forward User DMs To Bot"] = "Reenviar DMs del usuario al bot",
            ["Global Sudo List"] = "Lista de sudos globales",
            ["Hello Response"] = "Respuesta de saludo",
            ["Logging Channels"] = "Canales de registro",
            ["Stream"] = "Stream",
            ["Donation"] = "Donación",
            ["Module Blacklist"] = "Lista negra de módulos",
            ["Offline Emoji"] = "Emoji offline",
            ["Online Emoji"] = "Emoji online",
            ["Reply When Commands Are Not Allowed"] = "Responder cuando el comando no está permitido",
            ["Reply To Thanks"] = "Responder agradecimientos",
            ["Return PKMs"] = "Devolver PKM",
            ["Message Deletion Enabled"] = "Eliminación de mensajes activada",
            ["Error Message Delete Delay Seconds"] = "Retraso para eliminar mensajes de error",
            ["Delete User Command Messages"] = "Eliminar comandos de usuario",
            ["Role Can Clone"] = "Rol para clonar",
            ["Role Can Dump"] = "Rol para dump",
            ["Role Can Fix OT"] = "Rol para Fix OT",
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
            ["Bot Logo Sparkle Color 1"] = "Color de brillo del logo 1",
            ["Bot Logo Sparkle Color1"] = "Color de brillo del logo 1",
            ["Bot Logo Sparkle Color 2"] = "Color de brillo del logo 2",
            ["Bot Logo Sparkle Color2"] = "Color de brillo del logo 2",
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
