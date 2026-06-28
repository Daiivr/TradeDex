using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using static SysBot.Pokemon.TradeSettings;
using SysBot.Pokemon.Localization;

namespace SysBot.Pokemon;

public class DiscordSettings
{
    private const string Bot = nameof(Bot);

    private const string Channels = nameof(Channels);

    private const string Operation = nameof(Operation);

    private const string Roles = nameof(Roles);

    private const string Servers = nameof(Servers);

    private const string Startup = nameof(Startup);

    private const string Users = nameof(Users);

    private const string Appearance = nameof(Appearance);

    private const string Emojis = nameof(Emojis);

    private const string Moderation = nameof(Moderation);

    public enum EmbedColorOption
    {
        Blue,

        Green,

        Red,

        Gold,

        Purple,

        Teal,

        Orange,

        Magenta,

        LightGrey,

        DarkGrey
    }

    public enum ThumbnailOption
    {
        Gengar,

        Pikachu,

        Umbreon,

        Sylveon,

        Charmander,

        Jigglypuff,

        Flareon,

        Custom
    }

    [Category(Bot), TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Bot"), Description("Ajustes principales del bot de Discord.")]
    [JsonIgnore]
    public BotSettingsCategory BotSettings => new(this);

    [Category(Roles), TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Roles"), Description("Ajustes de roles que controlan acceso y prioridad en Discord.")]
    [JsonIgnore]
    public RoleSettingsCategory RoleSettings => new(this);

    [Category(Channels), TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Canales"), Description("Ajustes de canales usados por el bot en Discord.")]
    [JsonIgnore]
    public ChannelSettingsCategory ChannelSettings => new(this);

    [Category(Emojis), TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Emojis"), Description("Ajustes de emojis usados por el bot en Discord.")]
    [JsonIgnore]
    public EmojiSettingsCategory EmojiSettings => new(this);

    [Category(Moderation), TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Moderacion"), Description("Ajustes de listas negras, sudo y manejo de mensajes en Discord.")]
    [JsonIgnore]
    public ModerationSettingsCategory ModerationSettings => new(this);

    [Browsable(false), Category(Bot), Description("Token de inicio de sesión del bot.")]
    public string Token { get; set; } = string.Empty;

    [Browsable(false), Category(Moderation), Description("Texto adicional para agregar al comienzo del Embed."), DisplayName("Texto adicional del embed")]
    public string[] AdditionalEmbedText { get; set; } = [];

    [Browsable(false), Category(Moderation), Description("Deshabilitar esto eliminará la compatibilidad global con sudo.")]
    public bool AllowGlobalSudo { get; set; } = true;

    [Browsable(false), Category(Channels), Description("Canales que registrarán mensajes especiales, como anuncios."), DisplayName("Canales de Anuncios")]
    public RemoteControlAccessList AnnouncementChannels { get; set; } = new();

    [Browsable(false), Category(Channels), DisplayName("Canales de Registro de Abusos"), Description("Canales que registrarán los mensajes de abusos.")]
    public RemoteControlAccessList AbuseLogChannels { get; set; } = new();

    [Browsable(false), Category(Channels), DisplayName("Ajustes de los Anuncios")]
    public AnnouncementSettingsCategory AnnouncementSettings { get; set; } = new();

    [Browsable(false), Category(Bot), Description("Indica el color del estado de presencia de Discord solo considerando los bots que son de tipo Trade.")]
    public bool BotColorStatusTradeOnly { get; set; } = true;

    [Browsable(false), Category(Bot), Description("Enviará un estado embed para cuando el bot este online/offline a todos los canales incluidos en la lista blanca.")]
    public bool BotEmbedStatus { get; set; } = true;

    [Browsable(false), Category(Bot), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuraciones relacionadas con el estado del canal.")]
    public ChannelStatusSettings ChannelStatusConfig { get; set; } = new();

    [Browsable(false), Category(Bot), Description("Texto que se mostrara como estado personalizado del bot, junto al avatar."), DisplayName("Estado personalizado del bot")]
    public string BotGameStatus { get; set; } = "Trading Pokémon";

    [Category(Operation), Description("Habilita o deshabilita el sistema de XP para usuarios cuando usan comandos."), DisplayName("Sistema de XP")]
    public bool EnableXPSystem { get; set; } = false;

    [Browsable(false), Category(Emojis), TypeConverter(typeof(ExpandableObjectConverter)), Description("Emojis que se usan en mensajes visibles de Discord. Si un valor se deja vacio, se usa el icono predeterminado."), DisplayName("Iconos de mensajes")]
    public MessageIconSettings MessageIcons { get; set; } = new();

    private List<Badge> _customBadgeEmojis = GetDefaultBadgeEmojis();
    private List<LeagueEmoji> _leagueEmojis = GetDefaultLeagueEmojis();

    [Browsable(false), Category(Emojis), Description("Lista de emojis personalizados para insignias que se entregan al usuario luego de completar cierta cantidad de trades."), DisplayName("Insignias")]
    public List<Badge> CustomBadgeEmojis
    {
        get
        {
            EnsureDefaultBadges(_customBadgeEmojis);
            return _customBadgeEmojis;
        }
        set
        {
            _customBadgeEmojis = value ?? [];
            EnsureDefaultBadges(_customBadgeEmojis);
        }
    }

    [Browsable(false), Category(Emojis), Description("Lista de ligas del perfil segun el nivel de XP del usuario. Puedes usar emojis normales o emojis custom de Discord como <:bronze:1234567890>."), DisplayName("Ligas")]
    public List<LeagueEmoji> LeagueEmojis
    {
        get
        {
            EnsureDefaultLeagues(_leagueEmojis);
            return _leagueEmojis;
        }
        set
        {
            _leagueEmojis = value ?? [];
            EnsureDefaultLeagues(_leagueEmojis);
        }
    }

    private static List<Badge> GetDefaultBadgeEmojis() =>
    [
        new(1, "🏅"),
        new(50, "🎖️"),
        new(100, "🥉"),
        new(150, "🥈"),
        new(200, "🥇"),
        new(250, "🏆"),
        new(300, "👑"),
        new(350, "💎"),
        new(400, "🔥"),
        new(450, "🌟"),
        new(500, "💠"),
        new(550, "🔶"),
        new(600, "🛡️"),
        new(650, "🪙"),
        new(700, "⚔️"),
    ];

    private static void EnsureDefaultBadges(List<Badge> badges)
    {
        foreach (var defaultBadge in GetDefaultBadgeEmojis())
        {
            if (badges.All(b => b.TradeCount != defaultBadge.TradeCount))
                badges.Add(defaultBadge);
        }

        badges.Sort((left, right) => left.TradeCount.CompareTo(right.TradeCount));
    }

    private static List<LeagueEmoji> GetDefaultLeagueEmojis() =>
    [
        new("Bronze", 1, "🥉"),
        new("Silver", 5, "🥈"),
        new("Gold", 10, "🛡️"),
        new("Pearl", 20, "🔮"),
        new("Ruby", 35, "♦️"),
        new("Diamond", 50, "💎"),
        new("Master", 75, "🏆"),
    ];

    private static void EnsureDefaultLeagues(List<LeagueEmoji> leagues)
    {
        foreach (var defaultLeague in GetDefaultLeagueEmojis())
        {
            if (leagues.All(l => !string.Equals(l.Key, defaultLeague.Key, StringComparison.OrdinalIgnoreCase)))
                leagues.Add(defaultLeague);
        }

        foreach (var league in leagues)
        {
            if (league.RequiredLevel <= 0)
                league.RequiredLevel = GetDefaultLeagueEmojis().FirstOrDefault(l => string.Equals(l.Key, league.Key, StringComparison.OrdinalIgnoreCase))?.RequiredLevel ?? 1;
            if (string.IsNullOrWhiteSpace(league.Emoji))
                league.Emoji = GetDefaultLeagueEmojis().FirstOrDefault(l => string.Equals(l.Key, league.Key, StringComparison.OrdinalIgnoreCase))?.Emoji ?? "🏆";
        }

        leagues.Sort((left, right) =>
        {
            var order = GetLeagueProgressionOrder(left.Key).CompareTo(GetLeagueProgressionOrder(right.Key));
            return order != 0 ? order : left.RequiredLevel.CompareTo(right.RequiredLevel);
        });
    }

    private static int GetLeagueProgressionOrder(string? key)
    {
        var value = key?.Trim() ?? string.Empty;
        if (value.Contains("bronze", StringComparison.OrdinalIgnoreCase) || value.Contains("bronce", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (value.Contains("silver", StringComparison.OrdinalIgnoreCase) || value.Contains("plata", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (value.Contains("gold", StringComparison.OrdinalIgnoreCase) || value.Contains("oro", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (value.Contains("pearl", StringComparison.OrdinalIgnoreCase) || value.Contains("perla", StringComparison.OrdinalIgnoreCase))
            return 3;
        if (value.Contains("ruby", StringComparison.OrdinalIgnoreCase) || value.Contains("rubi", StringComparison.OrdinalIgnoreCase))
            return 4;
        if (value.Contains("diamond", StringComparison.OrdinalIgnoreCase) || value.Contains("diamante", StringComparison.OrdinalIgnoreCase))
            return 5;
        if (value.Contains("master", StringComparison.OrdinalIgnoreCase))
            return 6;
        return 99;
    }

    [Browsable(false), Category(Bot), Description("Agregara un emoji online/offline al nombre del canal segun el estado actual. Solo canales en lista blanca."), DisplayName("Estado del canal")]
    public bool ChannelStatus { get; set; } = true;

    [Browsable(false), Category(Channels), Description("Los canales con estos ID son los únicos canales donde el bot reconoce comandos.")]
    public RemoteControlAccessList ChannelWhitelist { get; set; } = new();

    [Browsable(false), Category(Bot), Description("Prefijo de comando del bot.")]
    public string CommandPrefix { get; set; } = "$";

    [Browsable(false), Category(Bot), Description("Cuando esta en True, permite usar cualquiera de estos prefijos: $ ! . = % ~ - + , / ? * ^ < > ` ; :\nSi esta en False, vuelve al prefijo predeterminado con un mensaje indicando el prefijo correcto."), DisplayName("Permitir cualquier prefijo")]
    public bool AllowAnyPrefix { get; set; } = false;

    [Category(Operation), Description("El bot puede responder con un conjunto de showdown en cualquier canal que el bot pueda ver, en lugar de solo los canales en los que el bot ha sido incluido en la lista blanca para ejecutarse. Haga esto solo si desea que el bot tenga más utilidad en canales que no son de bot.")]
    public bool ConvertPKMReplyAnyChannel { get; set; } = false;

    [Category(Operation), Description("El bot escucha los mensajes del canal para responder con un Showdown Set cada vez que se adjunta un archivo PKM (no con un comando).")]
    public bool ConvertPKMToShowdownSet { get; set; } = true;

    [Browsable(false), Category(Channels), Description("ID de usuario o de canal al que se reenviarán los DMs del bot. Déjalo vacío para desactivar."), DisplayName("Reenviar DMs")]
    public string UserDMsToBotForwarder { get; set; } = string.Empty;

    [Browsable(false), Category(Moderation), Description("ID de usuario de Discord separados por comas que tendrán acceso sudo al Bot Hub."), DisplayName("Lista de Sudos Globales")]
    public RemoteControlAccessList GlobalSudoList { get; set; } = new();

    [Browsable(false), Category(Moderation), Description("Mensaje personalizado con el que el bot responderá cuando un usuario lo salude. Utilice formato de cadena para mencionar al usuario en la respuesta.")]
    public string HelloResponse { get; set; } = "Hi {0}!";

    [Browsable(false), Category(Channels), Description("ID de canal que harán eco de los datos del bot de registro."), DisplayName("Canales de Registros")]
    public RemoteControlAccessList LoggingChannels { get; set; } = new();

    [Category(Operation), TypeConverter(typeof(ExpandableObjectConverter)), Description("Opciones extras sobre el stream del host."), DisplayName("Opciones del Stream")]
    public StreamOptions Stream { get; set; } = new();

    [Category(Operation), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuracion de donaciones."), DisplayName("Opciones de Donacion")]
    public DonationOptions Donation { get; set; } = new();

    [Browsable(false), Category(Moderation), Description("Lista de módulos que no se cargarán cuando se inicie el bot (separados por comas).")]
    public string ModuleBlacklist { get; set; } = string.Empty;

    [Browsable(false), Category(Emojis), Description("Emoji personalizado para usar cuando el bot está offline.")]
    public string OfflineEmoji { get; set; } = "❌";

    [Browsable(false), Category(Emojis), Description("Emoji personalizado para usar cuando el bot está online.")]
    public string OnlineEmoji { get; set; } = "✅";

    [Browsable(false), Category(Moderation), Description("Responde a los usuarios si no se les permite utilizar un comando determinado en el canal. Cuando es falso, el bot los ignorará silenciosamente.")]
    public bool ReplyCannotUseCommandInChannel { get; set; } = true;

    [Browsable(false), Category(Moderation), Description("Enviará una respuesta aleatoria a un usuario que agradezca al bot.")]
    public bool ReplyToThanks { get; set; } = false;

    [Browsable(false), Category(Moderation), Description("Devuelve al usuario los archivos PKM de Pokémon mostrados en el intercambio.")]
    public bool ReturnPKMs { get; set; } = true;

    [Browsable(false), Category(Moderation), Description("Cuando esta habilitado, el bot eliminara automaticamente mensajes de error y comandos de usuario despues de un retraso. Deshabilitalo para conservar todos los mensajes permanentemente."), DisplayName("Eliminacion de mensajes")]
    public bool MessageDeletionEnabled { get; set; } = true;

    [Browsable(false), Category(Moderation), Description("Numero de segundos a esperar antes de eliminar mensajes de error/respuesta del bot. Solo aplica si MessageDeletionEnabled esta en true."), DisplayName("Retraso para eliminar mensajes")]
    public int ErrorMessageDeleteDelaySeconds { get; set; } = 10;

    [Browsable(false), Category(Moderation), Description("Cuando esta habilitado, los mensajes de comandos del usuario se eliminaran junto con las respuestas del bot. Deshabilitalo para mantener visibles los comandos."), DisplayName("Eliminar comandos de usuario")]
    public bool DeleteUserCommandMessages { get; set; } = true;

    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola de clonación.")]
    public RemoteControlAccessList RoleCanClone { get; set; } = new() { AllowIfEmpty = true };

    [Browsable(false), Category(Roles), Description("Los usuarios con esta función pueden ingresar a la cola de Dump.")]
    public RemoteControlAccessList RoleCanDump { get; set; } = new() { AllowIfEmpty = true };

    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola Fix OT.")]
    public RemoteControlAccessList RoleCanFixOT { get; set; } = new() { AllowIfEmpty = true };

    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola de verificación de semillas/solicitudes especiales.")]
    public RemoteControlAccessList RoleCanSeedCheckorSpecialRequest { get; set; } = new() { AllowIfEmpty = true };

    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola de Trade.")]
    public RemoteControlAccessList RoleCanTrade { get; set; } = new() { AllowIfEmpty = true };

    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden utilizar las funciones Trade adicionales.")]
    public RemoteControlAccessList RoleCanTradePlus { get; set; } = new() { AllowIfEmpty = true };

    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden unirse a la cola con una mejor posición.")]
    public RemoteControlAccessList RoleFavored { get; set; } = new() { AllowIfEmpty = false };

    // Whitelists
    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden controlar de forma remota la consola (si la ejecutan como Remote Control Bot).")]
    public RemoteControlAccessList RoleRemoteControl { get; set; } = new() { AllowIfEmpty = false };

    [Browsable(false), Category(Roles), Description("Los usuarios con este rol pueden omitir las restricciones de comandos.")]
    public RemoteControlAccessList RoleSudo { get; set; } = new() { AllowIfEmpty = false };

    // Operation
    [Browsable(false), Category(Moderation), Description("Los servidores con estos ID no podrán utilizar el bot abandonará el servidor.")]
    public RemoteControlAccessList ServerBlacklist { get; set; } = new() { AllowIfEmpty = false };

    [Browsable(false), Category(Channels), Description("Canales de registro que registrarán mensajes de inicio de operaciones.")]
    public RemoteControlAccessList TradeStartingChannels { get; set; } = new();

    [Browsable(false), Category(Channels), Description("Canal que registrara informacion detallada de errores de trade, incluyendo solicitudes del usuario y razones de fallo."), DisplayName("Canal de log completo de errores de trade")]
    public RemoteControlAccessList FullTradeErrorLogChannels { get; set; } = new();

    // Startup
    [Browsable(false), Category(Moderation), Description("Los usuarios con estos ID de usuario no pueden utilizar el bot.")]
    public RemoteControlAccessList UserBlacklist { get; set; } = new();

    public override string ToString() => "Configuración de integración de Discord";

    public sealed class BotSettingsCategory(DiscordSettings settings)
    {
        public override string ToString() => "Ajustes del bot";

        [Description("Token de inicio de sesión del bot.")]
        public string Token
        {
            get => settings.Token;
            set => settings.Token = value;
        }

        [Description("Prefijo de comando del bot.")]
        public string CommandPrefix
        {
            get => settings.CommandPrefix;
            set => settings.CommandPrefix = value;
        }

        [Description("Cuando esta en True, permite usar cualquiera de estos prefijos: $ ! . = % ~ - + , / ? * ^ < > ` ; :\nSi esta en False, vuelve al prefijo predeterminado con un mensaje indicando el prefijo correcto."), DisplayName("Permitir cualquier prefijo")]
        public bool AllowAnyPrefix
        {
            get => settings.AllowAnyPrefix;
            set => settings.AllowAnyPrefix = value;
        }

        [Description("Texto que se mostrara como estado personalizado del bot, junto al avatar."), DisplayName("Estado personalizado del bot")]
        public string BotGameStatus
        {
            get => settings.BotGameStatus;
            set => settings.BotGameStatus = value;
        }

        [Description("Enviará un estado embed para cuando el bot este online/offline a todos los canales incluidos en la lista blanca.")]
        public bool BotEmbedStatus
        {
            get => settings.BotEmbedStatus;
            set => settings.BotEmbedStatus = value;
        }

        [Description("Indica el color del estado de presencia de Discord solo considerando los bots que son de tipo Trade.")]
        public bool BotColorStatusTradeOnly
        {
            get => settings.BotColorStatusTradeOnly;
            set => settings.BotColorStatusTradeOnly = value;
        }

        [Description("Agregara un emoji online/offline al nombre del canal segun el estado actual. Solo canales en lista blanca."), DisplayName("Estado del canal")]
        public bool ChannelStatus
        {
            get => settings.ChannelStatus;
            set => settings.ChannelStatus = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuraciones relacionadas con el estado del canal.")]
        public ChannelStatusSettings ChannelStatusConfig
        {
            get => settings.ChannelStatusConfig;
            set => settings.ChannelStatusConfig = value;
        }

    }

    public sealed class RoleSettingsCategory(DiscordSettings settings)
    {
        public override string ToString() => "Ajustes de roles";

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden ingresar a la cola de clonación.")]
        public RemoteControlAccessList RoleCanClone
        {
            get => settings.RoleCanClone;
            set => settings.RoleCanClone = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con esta función pueden ingresar a la cola de Dump.")]
        public RemoteControlAccessList RoleCanDump
        {
            get => settings.RoleCanDump;
            set => settings.RoleCanDump = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden ingresar a la cola Fix OT.")]
        public RemoteControlAccessList RoleCanFixOT
        {
            get => settings.RoleCanFixOT;
            set => settings.RoleCanFixOT = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden ingresar a la cola de verificación de semillas/solicitudes especiales.")]
        public RemoteControlAccessList RoleCanSeedCheckorSpecialRequest
        {
            get => settings.RoleCanSeedCheckorSpecialRequest;
            set => settings.RoleCanSeedCheckorSpecialRequest = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden ingresar a la cola de Trade.")]
        public RemoteControlAccessList RoleCanTrade
        {
            get => settings.RoleCanTrade;
            set => settings.RoleCanTrade = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden utilizar las funciones Trade adicionales.")]
        public RemoteControlAccessList RoleCanTradePlus
        {
            get => settings.RoleCanTradePlus;
            set => settings.RoleCanTradePlus = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden unirse a la cola con una mejor posición.")]
        public RemoteControlAccessList RoleFavored
        {
            get => settings.RoleFavored;
            set => settings.RoleFavored = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden controlar de forma remota la consola (si la ejecutan como Remote Control Bot).")]
        public RemoteControlAccessList RoleRemoteControl
        {
            get => settings.RoleRemoteControl;
            set => settings.RoleRemoteControl = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con este rol pueden omitir las restricciones de comandos.")]
        public RemoteControlAccessList RoleSudo
        {
            get => settings.RoleSudo;
            set => settings.RoleSudo = value;
        }
    }

    public sealed class ChannelSettingsCategory(DiscordSettings settings)
    {
        public override string ToString() => "Ajustes de canales";

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Canales que registrarán mensajes especiales, como anuncios."), DisplayName("Canales de Anuncios")]
        public RemoteControlAccessList AnnouncementChannels
        {
            get => settings.AnnouncementChannels;
            set => settings.AnnouncementChannels = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Canales de Registro de Abusos"), Description("Canales que registrarán los mensajes de abusos.")]
        public RemoteControlAccessList AbuseLogChannels
        {
            get => settings.AbuseLogChannels;
            set => settings.AbuseLogChannels = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), DisplayName("Ajustes de los Anuncios")]
        public AnnouncementSettingsCategory AnnouncementSettings
        {
            get => settings.AnnouncementSettings;
            set => settings.AnnouncementSettings = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los canales con estos ID son los únicos canales donde el bot reconoce comandos.")]
        public RemoteControlAccessList ChannelWhitelist
        {
            get => settings.ChannelWhitelist;
            set => settings.ChannelWhitelist = value;
        }

        [Description("ID de usuario o de canal al que se reenviarán los DMs del bot. Déjalo vacío para desactivar."), DisplayName("Reenviar DMs")]
        public string UserDMsToBotForwarder
        {
            get => settings.UserDMsToBotForwarder;
            set => settings.UserDMsToBotForwarder = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("ID de canal que harán eco de los datos del bot de registro."), DisplayName("Canales de Registros")]
        public RemoteControlAccessList LoggingChannels
        {
            get => settings.LoggingChannels;
            set => settings.LoggingChannels = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Canales de registro que registrarán mensajes de inicio de operaciones.")]
        public RemoteControlAccessList TradeStartingChannels
        {
            get => settings.TradeStartingChannels;
            set => settings.TradeStartingChannels = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Canal que registrara informacion detallada de errores de trade, incluyendo solicitudes del usuario y razones de fallo."), DisplayName("Canal de log completo de errores de trade")]
        public RemoteControlAccessList FullTradeErrorLogChannels
        {
            get => settings.FullTradeErrorLogChannels;
            set => settings.FullTradeErrorLogChannels = value;
        }
    }

    public sealed class EmojiSettingsCategory(DiscordSettings settings)
    {
        public override string ToString() => "Ajustes de emojis";

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Emojis que se usan en mensajes visibles de Discord. Si un valor se deja vacio, se usa el icono predeterminado."), DisplayName("Iconos de mensajes")]
        public MessageIconSettings MessageIcons
        {
            get => settings.MessageIcons;
            set => settings.MessageIcons = value;
        }

        [Description("Lista de emojis personalizados para insignias que se entregan al usuario luego de completar cierta cantidad de trades."), DisplayName("Insignias")]
        public List<Badge> CustomBadgeEmojis
        {
            get => settings.CustomBadgeEmojis;
            set => settings.CustomBadgeEmojis = value;
        }

        [Description("Lista de ligas del perfil segun el nivel de XP del usuario. Puedes usar emojis normales o emojis custom de Discord como <:bronze:1234567890>."), DisplayName("Ligas")]
        public List<LeagueEmoji> LeagueEmojis
        {
            get => settings.LeagueEmojis;
            set => settings.LeagueEmojis = value;
        }

        [Description("Emoji personalizado para usar cuando el bot está offline.")]
        public string OfflineEmoji
        {
            get => settings.OfflineEmoji;
            set => settings.OfflineEmoji = value;
        }

        [Description("Emoji personalizado para usar cuando el bot está online.")]
        public string OnlineEmoji
        {
            get => settings.OnlineEmoji;
            set => settings.OnlineEmoji = value;
        }
    }

    public sealed class ModerationSettingsCategory(DiscordSettings settings)
    {
        public override string ToString() => "Ajustes de moderacion";

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los servidores con estos ID no podrán utilizar el bot abandonará el servidor.")]
        public RemoteControlAccessList ServerBlacklist
        {
            get => settings.ServerBlacklist;
            set => settings.ServerBlacklist = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("Los usuarios con estos ID de usuario no pueden utilizar el bot.")]
        public RemoteControlAccessList UserBlacklist
        {
            get => settings.UserBlacklist;
            set => settings.UserBlacklist = value;
        }

        [Description("Lista de módulos que no se cargarán cuando se inicie el bot (separados por comas).")]
        public string ModuleBlacklist
        {
            get => settings.ModuleBlacklist;
            set => settings.ModuleBlacklist = value;
        }

        [Description("Deshabilitar esto eliminará la compatibilidad global con sudo.")]
        public bool AllowGlobalSudo
        {
            get => settings.AllowGlobalSudo;
            set => settings.AllowGlobalSudo = value;
        }

        [TypeConverter(typeof(ExpandableObjectConverter)), Description("ID de usuario de Discord separados por comas que tendrán acceso sudo al Bot Hub."), DisplayName("Lista de Sudos Globales")]
        public RemoteControlAccessList GlobalSudoList
        {
            get => settings.GlobalSudoList;
            set => settings.GlobalSudoList = value;
        }

        [Description("Texto adicional para agregar al comienzo del Embed."), DisplayName("Texto adicional del embed")]
        public string[] AdditionalEmbedText
        {
            get => settings.AdditionalEmbedText;
            set => settings.AdditionalEmbedText = value;
        }

        [Description("Mensaje personalizado con el que el bot responderá cuando un usuario lo salude. Utilice formato de cadena para mencionar al usuario en la respuesta.")]
        public string HelloResponse
        {
            get => settings.HelloResponse;
            set => settings.HelloResponse = value;
        }

        [Description("Responde a los usuarios si no se les permite utilizar un comando determinado en el canal. Cuando es falso, el bot los ignorará silenciosamente.")]
        public bool ReplyCannotUseCommandInChannel
        {
            get => settings.ReplyCannotUseCommandInChannel;
            set => settings.ReplyCannotUseCommandInChannel = value;
        }

        [Description("Enviará una respuesta aleatoria a un usuario que agradezca al bot.")]
        public bool ReplyToThanks
        {
            get => settings.ReplyToThanks;
            set => settings.ReplyToThanks = value;
        }

        [Description("Devuelve al usuario los archivos PKM de Pokémon mostrados en el intercambio.")]
        public bool ReturnPKMs
        {
            get => settings.ReturnPKMs;
            set => settings.ReturnPKMs = value;
        }

        [Description("Cuando esta habilitado, el bot eliminara automaticamente mensajes de error y comandos de usuario despues de un retraso. Deshabilitalo para conservar todos los mensajes permanentemente."), DisplayName("Eliminacion de mensajes")]
        public bool MessageDeletionEnabled
        {
            get => settings.MessageDeletionEnabled;
            set => settings.MessageDeletionEnabled = value;
        }

        [Description("Numero de segundos a esperar antes de eliminar mensajes de error/respuesta del bot. Solo aplica si MessageDeletionEnabled esta en true."), DisplayName("Retraso para eliminar mensajes")]
        public int ErrorMessageDeleteDelaySeconds
        {
            get => settings.ErrorMessageDeleteDelaySeconds;
            set => settings.ErrorMessageDeleteDelaySeconds = value;
        }

        [Description("Cuando esta habilitado, los mensajes de comandos del usuario se eliminaran junto con las respuestas del bot. Deshabilitalo para mantener visibles los comandos."), DisplayName("Eliminar comandos de usuario")]
        public bool DeleteUserCommandMessages
        {
            get => settings.DeleteUserCommandMessages;
            set => settings.DeleteUserCommandMessages = value;
        }
    }

    public class ChannelStatusSettings
    {
        public override string ToString() => "Configuraciones relacionadas con el estado del canal.";

        [Description("Añade emoji online/offline al nombre del canal en funcion de su estado actual. Solo canales en lista blanca."), DisplayName("Activar el estado del canal")]
        public bool EnableChannelStatus { get; set; } = false;

        [Description("Emoji personalizado para usar cuando el bot esta online.")]
        public string OnlineEmoji { get; set; } = "✅";

        [Description("Emoji personalizado para usar cuando el bot esta offline.")]
        public string OfflineEmoji { get; set; } = "❌";
    }

    public class MessageIconSettings
    {
        public const string DefaultWarning = "⚠️";
        public const string DefaultSuccess = "✅";
        public const string DefaultError = "❌";
        public const string DefaultDirectMessage = "📩";
        public const string DefaultWaiting = "⌛";

        public override string ToString() => "Iconos de mensajes";

        [Description("Emoji usado para avisos o advertencias.")]
        public string Warning { get; set; } = DefaultWarning;

        [Description("Emoji usado para acciones exitosas.")]
        public string Success { get; set; } = DefaultSuccess;

        [Description("Emoji usado para errores o acciones fallidas.")]
        public string Error { get; set; } = DefaultError;

        [Description("Emoji usado cuando se envia o intenta enviar un mensaje directo.")]
        public string DirectMessage { get; set; } = DefaultDirectMessage;

        [Description("Emoji usado para esperas, expiraciones o temporizadores.")]
        public string Waiting { get; set; } = DefaultWaiting;

        [JsonIgnore, Browsable(false)]
        public string WarningOrDefault => GetOrDefault(Warning, DefaultWarning);

        [JsonIgnore, Browsable(false)]
        public string SuccessOrDefault => GetOrDefault(Success, DefaultSuccess);

        [JsonIgnore, Browsable(false)]
        public string ErrorOrDefault => GetOrDefault(Error, DefaultError);

        [JsonIgnore, Browsable(false)]
        public string DirectMessageOrDefault => GetOrDefault(DirectMessage, DefaultDirectMessage);

        [JsonIgnore, Browsable(false)]
        public string WaitingOrDefault => GetOrDefault(Waiting, DefaultWaiting);

        private static string GetOrDefault(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    public class StreamOptions
    {
        public override string ToString() => "(Collection)";

        [Category(Operation), Description("Enlace de transmision."), DisplayName("Link al Stream")]
        public string StreamLink { get; set; } = string.Empty;

        [Category(Operation), Description("Opcion de icono para la transmision."), DisplayName("Icono de la plataforma de Stream")]
        public StreamIconOption StreamIcon { get; set; } = StreamIconOption.Twitch;

        public static readonly Dictionary<StreamIconOption, string> StreamIconUrls = new()
        {
            { StreamIconOption.Twitch, "https://i.imgur.com/zD95Rzy.png" },
            { StreamIconOption.Youtube, "https://i.imgur.com/VzFGPdo.png" },
            { StreamIconOption.Facebook, "https://i.imgur.com/YYkD2fe.png" },
            { StreamIconOption.Kick, "https://i.imgur.com/HH8AAJY.jpg" },
            { StreamIconOption.TikTok, "https://i.imgur.com/Jm89lHP.png" },
        };
    }

    public class DonationOptions
    {
        public override string ToString() => "(Collection)";

        [Category(Operation), Description("Enlace de donacion."), DisplayName("Link para Donaciones")]
        public string DonationLink { get; set; } = string.Empty;

        [Category(Operation), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuracion de la barra de progreso."), DisplayName("Configuracion Extra")]
        public ProgressBarSettings ProgressBar { get; set; } = new();

        public class ProgressBarSettings
        {
            public override string ToString() => "(Configuracion de Barra de Donaciones)";

            [Category(Operation), Description("Activa o desactiva la barra de progreso de donaciones."), DisplayName("Mostrar Barra de Progreso")]
            public bool ShowProgressBar { get; set; } = false;

            [Category(Operation), Description("Meta de donacion."), DisplayName("Meta de Donaciones")]
            public string DonationGoal { get; set; } = string.Empty;

            [Category(Operation), Description("Donaciones actuales."), DisplayName("Donaciones Actuales")]
            public string DonationCurrent { get; set; } = string.Empty;
        }
    }

    [Category(Operation), TypeConverter(typeof(CategoryConverter<AnnouncementSettingsCategory>))]
    public class AnnouncementSettingsCategory
    {
        public EmbedColorOption AnnouncementEmbedColor { get; set; } = EmbedColorOption.Purple;

        [Category("Embed Settings"), Description("Opción de miniatura para anuncios.")]
        public ThumbnailOption AnnouncementThumbnailOption { get; set; } = ThumbnailOption.Gengar;

        [Category("Embed Settings"), Description("URL en miniatura personalizada para anuncios.")]
        public string CustomAnnouncementThumbnailUrl { get; set; } = string.Empty;

        [Category("Embed Settings"), Description("Habilite la selección aleatoria de colores para los anuncios.")]
        public bool RandomAnnouncementColor { get; set; } = false;

        [Category("Embed Settings"), Description("Habilite la selección aleatoria de miniaturas para anuncios.")]
        public bool RandomAnnouncementThumbnail { get; set; } = false;

        public override string ToString() => "Configuración de anuncios";
    }
}

public enum StreamIconOption
{
    Twitch,
    Youtube,
    Facebook,
    Kick,
    TikTok,
}

public class Badge(int tradeCount, string emoji)
{
    public int TradeCount { get; } = tradeCount;
    public string Emoji { get; set; } = emoji;

    public override string ToString() => Emoji;
}

public class LeagueEmoji
{
    public LeagueEmoji() { }

    public LeagueEmoji(string key, int requiredLevel, string emoji)
    {
        Key = key;
        RequiredLevel = requiredLevel;
        Emoji = emoji;
    }

    public string Key { get; set; } = string.Empty;
    public int RequiredLevel { get; set; }
    public string Emoji { get; set; } = string.Empty;

    public override string ToString() => $"{Emoji} {Key} (Level {RequiredLevel}+)";
}
