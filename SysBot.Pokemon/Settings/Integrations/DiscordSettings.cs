using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using static SysBot.Pokemon.TradeSettings;

namespace SysBot.Pokemon;

public class DiscordSettings
{
    private const string Channels = nameof(Channels);

    private const string Operation = nameof(Operation);

    private const string Roles = nameof(Roles);

    private const string Servers = nameof(Servers);

    private const string Startup = nameof(Startup);

    private const string Users = nameof(Users);

    private const string Appearance = nameof(Appearance);

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

    [Category(Startup), Description("Token de inicio de sesión del bot.")]
    public string Token { get; set; } = string.Empty;

    [Category(Operation), Description("Texto adicional para agregar al comienzo del Embed."), DisplayName("Texto adicional del embed")]
    public string[] AdditionalEmbedText { get; set; } = [];

    [Category(Users), Description("Deshabilitar esto eliminará la compatibilidad global con sudo.")]
    public bool AllowGlobalSudo { get; set; } = true;

    [Category(Channels), Description("Canales que registrarán mensajes especiales, como anuncios."), DisplayName("Canales de Anuncios")]
    public RemoteControlAccessList AnnouncementChannels { get; set; } = new();

    [Category(Channels), DisplayName("Canales de Registro de Abusos"), Description("Canales que registrarán los mensajes de abusos.")]
    public RemoteControlAccessList AbuseLogChannels { get; set; } = new();

    [Category(Channels), DisplayName("Ajustes de los Anuncios")]
    public AnnouncementSettingsCategory AnnouncementSettings { get; set; } = new();

    [Category(Startup), Description("Indica el color del estado de presencia de Discord solo considerando los bots que son de tipo Trade.")]
    public bool BotColorStatusTradeOnly { get; set; } = true;

    [Category(Startup), Description("Enviará un estado embed para cuando el bot este online/offline a todos los canales incluidos en la lista blanca.")]
    public bool BotEmbedStatus { get; set; } = true;

    [Category(Startup), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuraciones relacionadas con el estado del canal.")]
    public ChannelStatusSettings ChannelStatusConfig { get; set; } = new();

    [Category(Startup), Description("Estado personalizado del bot."), DisplayName("Estado de Juego del Bot")]
    public string BotGameStatus { get; set; } = "Trading Pokémon";

    [Category(Operation), Description("Habilita o deshabilita el sistema de XP para usuarios cuando usan comandos."), DisplayName("Sistema de XP")]
    public bool EnableXPSystem { get; set; } = false;

    [Category(Appearance), TypeConverter(typeof(ExpandableObjectConverter)), Description("Emojis que se usan en mensajes visibles de Discord. Si un valor se deja vacio, se usa el icono predeterminado."), DisplayName("Iconos de mensajes")]
    public MessageIconSettings MessageIcons { get; set; } = new();

    private List<Badge> _customBadgeEmojis = GetDefaultBadgeEmojis();

    [Category("Insignias"), Description("Lista de emojis personalizados para insignias que se entregan al usuario luego de completar cierta cantidad de trades."), DisplayName("Insignias")]
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

    [Category(Startup), Description("Agregara un emoji online/offline al nombre del canal segun el estado actual. Solo canales en lista blanca."), DisplayName("Estado del canal")]
    public bool ChannelStatus { get; set; } = true;

    [Category(Channels), Description("Los canales con estos ID son los únicos canales donde el bot reconoce comandos.")]
    public RemoteControlAccessList ChannelWhitelist { get; set; } = new();

    [Category(Startup), Description("Prefijo de comando del bot.")]
    public string CommandPrefix { get; set; } = "$";

    [Category(Startup), Description("Cuando esta en True, permite usar cualquiera de estos prefijos: $ ! . = % ~ - + , / ? * ^ < > ` ; :\nSi esta en False, vuelve al prefijo predeterminado con un mensaje indicando el prefijo correcto."), DisplayName("Permitir cualquier prefijo")]
    public bool AllowAnyPrefix { get; set; } = false;

    [Category(Operation), Description("El bot puede responder con un conjunto de showdown en cualquier canal que el bot pueda ver, en lugar de solo los canales en los que el bot ha sido incluido en la lista blanca para ejecutarse. Haga esto solo si desea que el bot tenga más utilidad en canales que no son de bot.")]
    public bool ConvertPKMReplyAnyChannel { get; set; } = false;

    [Category(Operation), Description("El bot escucha los mensajes del canal para responder con un Showdown Set cada vez que se adjunta un archivo PKM (no con un comando).")]
    public bool ConvertPKMToShowdownSet { get; set; } = true;

    [Category(Channels), Description("ID de usuario o de canal al que se reenviarán los DMs del bot. Déjalo vacío para desactivar."), DisplayName("Reenviar DMs")]
    public string UserDMsToBotForwarder { get; set; } = string.Empty;

    [Category(Users), Description("ID de usuario de Discord separados por comas que tendrán acceso sudo al Bot Hub."), DisplayName("Lista de Sudos Globales")]
    public RemoteControlAccessList GlobalSudoList { get; set; } = new();

    [Category(Operation), Description("Mensaje personalizado con el que el bot responderá cuando un usuario lo salude. Utilice formato de cadena para mencionar al usuario en la respuesta.")]
    public string HelloResponse { get; set; } = "Hi {0}!";

    [Category(Channels), Description("ID de canal que harán eco de los datos del bot de registro."), DisplayName("Canales de Registros")]
    public RemoteControlAccessList LoggingChannels { get; set; } = new();

    [Category(Operation), TypeConverter(typeof(ExpandableObjectConverter)), Description("Opciones extras sobre el stream del host."), DisplayName("Opciones del Stream")]
    public StreamOptions Stream { get; set; } = new();

    [Category(Operation), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuracion de donaciones."), DisplayName("Opciones de Donacion")]
    public DonationOptions Donation { get; set; } = new();

    [Category(Startup), Description("Lista de módulos que no se cargarán cuando se inicie el bot (separados por comas).")]
    public string ModuleBlacklist { get; set; } = string.Empty;

    [Description("Emoji personalizado para usar cuando el bot está offline.")]
    public string OfflineEmoji { get; set; } = "❌";

    [Description("Emoji personalizado para usar cuando el bot está online.")]
    public string OnlineEmoji { get; set; } = "✅";

    [Category(Operation), Description("Responde a los usuarios si no se les permite utilizar un comando determinado en el canal. Cuando es falso, el bot los ignorará silenciosamente.")]
    public bool ReplyCannotUseCommandInChannel { get; set; } = true;

    [Category(Operation), Description("Enviará una respuesta aleatoria a un usuario que agradezca al bot.")]
    public bool ReplyToThanks { get; set; } = false;

    [Category(Operation), Description("Devuelve al usuario los archivos PKM de Pokémon mostrados en el intercambio.")]
    public bool ReturnPKMs { get; set; } = true;

    [Category(Operation), Description("Cuando esta habilitado, el bot eliminara automaticamente mensajes de error y comandos de usuario despues de un retraso. Deshabilitalo para conservar todos los mensajes permanentemente."), DisplayName("Eliminacion de mensajes")]
    public bool MessageDeletionEnabled { get; set; } = true;

    [Category(Operation), Description("Numero de segundos a esperar antes de eliminar mensajes de error/respuesta del bot. Solo aplica si MessageDeletionEnabled esta en true."), DisplayName("Retraso para eliminar mensajes")]
    public int ErrorMessageDeleteDelaySeconds { get; set; } = 10;

    [Category(Operation), Description("Cuando esta habilitado, los mensajes de comandos del usuario se eliminaran junto con las respuestas del bot. Deshabilitalo para mantener visibles los comandos."), DisplayName("Eliminar comandos de usuario")]
    public bool DeleteUserCommandMessages { get; set; } = true;

    [Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola de clonación.")]
    public RemoteControlAccessList RoleCanClone { get; set; } = new() { AllowIfEmpty = true };

    [Category(Roles), Description("Los usuarios con esta función pueden ingresar a la cola de Dump.")]
    public RemoteControlAccessList RoleCanDump { get; set; } = new() { AllowIfEmpty = true };

    [Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola Fix OT.")]
    public RemoteControlAccessList RoleCanFixOT { get; set; } = new() { AllowIfEmpty = true };

    [Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola de verificación de semillas/solicitudes especiales.")]
    public RemoteControlAccessList RoleCanSeedCheckorSpecialRequest { get; set; } = new() { AllowIfEmpty = true };

    [Category(Roles), Description("Los usuarios con este rol pueden ingresar a la cola de Trade.")]
    public RemoteControlAccessList RoleCanTrade { get; set; } = new() { AllowIfEmpty = true };

    [Category(Roles), Description("Los usuarios con este rol pueden utilizar las funciones Trade adicionales.")]
    public RemoteControlAccessList RoleCanTradePlus { get; set; } = new() { AllowIfEmpty = true };

    [Category(Roles), Description("Los usuarios con este rol pueden unirse a la cola con una mejor posición.")]
    public RemoteControlAccessList RoleFavored { get; set; } = new() { AllowIfEmpty = false };

    // Whitelists
    [Category(Roles), Description("Los usuarios con este rol pueden controlar de forma remota la consola (si la ejecutan como Remote Control Bot).")]
    public RemoteControlAccessList RoleRemoteControl { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Los usuarios con este rol pueden omitir las restricciones de comandos.")]
    public RemoteControlAccessList RoleSudo { get; set; } = new() { AllowIfEmpty = false };

    // Operation
    [Category(Servers), Description("Los servidores con estos ID no podrán utilizar el bot abandonará el servidor.")]
    public RemoteControlAccessList ServerBlacklist { get; set; } = new() { AllowIfEmpty = false };

    [Category(Channels), Description("Canales de registro que registrarán mensajes de inicio de operaciones.")]
    public RemoteControlAccessList TradeStartingChannels { get; set; } = new();

    [Category(Channels), Description("Canal que registrara informacion detallada de errores de trade, incluyendo solicitudes del usuario y razones de fallo."), DisplayName("Canal de log completo de errores de trade")]
    public RemoteControlAccessList FullTradeErrorLogChannels { get; set; } = new();

    // Startup
    [Category(Users), Description("Los usuarios con estos ID de usuario no pueden utilizar el bot.")]
    public RemoteControlAccessList UserBlacklist { get; set; } = new();

    public override string ToString() => "Configuración de integración de Discord";

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
