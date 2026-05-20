using System.ComponentModel;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon;

public sealed class PokeTradeHubConfig : BaseConfig
{
    [Browsable(false)]
    private const string BotEncounter = nameof(BotEncounter);

    private const string BotTrade = nameof(BotTrade);

    private const string Integration = nameof(Integration);

    [Category(BotTrade), Description("Nombre del bot de Discord que está ejecutando el programa. Esto titulará la ventana para un reconocimiento más fácil. Requiere reiniciar el programa.")]
    public string BotName { get; set; } = string.Empty;

    [Category(BotTrade)]
    [Description("URL de imagen o directorio de archivo para un logo de 208x101 que se mostrará en la esquina superior izquierda. Requiere reiniciar el programa.")]
    public string BotLogoImage { get; set; } = string.Empty;

    [Category(BotTrade)]
    [Description("Primer color de brillo detrás del logo. Ingresar como RGB (ej. \"255, 20, 200\"). Dejar en blanco para usar la paleta neón predeterminada. Requiere reiniciar el programa.")]
    public string BotLogoSparkleColor1 { get; set; } = string.Empty;

    [Category(BotTrade)]
    [Description("Segundo color de brillo detrás del logo. Ingresar como RGB (ej. \"0, 200, 255\"). Dejar en blanco para usar la paleta neón predeterminada. Requiere reiniciar el programa.")]
    public string BotLogoSparkleColor2 { get; set; } = string.Empty;

    [Category(Integration)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public DiscordSettings Discord { get; set; } = new();

    [Category(BotTrade), Description("Configuración para intercambios de distribución en reposo.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public DistributionSettings Distribution { get; set; } = new();

    // Encounter Bots - Para encontrar o alojar Pokémon en el juego.
    [Browsable(false)]
    [Category(BotEncounter)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public EncounterSettings EncounterSWSH { get; set; } = new();

    [Category(Integration), Description("Permite que usuarios favorecidos entren a la cola con una posición más ventajosa que los no favorecidos.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public FavoredPrioritySettings Favoritism { get; set; } = new();

    [Category(Operation)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public QueueSettings Queues { get; set; } = new();

    [Browsable(false)]
    [Category(BotEncounter)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public RaidSettings RaidSWSH { get; set; } = new();

    [Browsable(false)]
    [Category(BotTrade)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public SeedCheckSettings SeedCheckSWSH { get; set; } = new();

    [Browsable(false)]
    public override bool Shuffled => Distribution.Shuffled;

    [Browsable(false)]
    [Category(BotEncounter), Description("Condiciones de detención para EncounterBot.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public StopConditionSettings StopConditions { get; set; } = new();

    [Category(Integration), Description("Configurar la generación de recursos para streaming.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public StreamSettings Stream { get; set; } = new();

    [Browsable(false)]
    [Category(Integration), Description("Opción de tema elegida por el usuario.")]
    public string ThemeOption { get; set; } = string.Empty;

    [Category(Operation), Description("Agregar tiempo adicional para consolas Switch más lentas.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TimingSettings Timings { get; set; } = new();

    // Trade Bots

    [Category(BotTrade)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TradeSettings Trade { get; set; } = new();

    [Category(BotTrade)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TradeAbuseSettings TradeAbuse { get; set; } = new();

    // Integration
    [Category(Integration)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TwitchSettings Twitch { get; set; } = new();

    [Category(Integration)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public YouTubeSettings YouTube { get; set; } = new();

    [Category(Operation), Description("Configuración para la recuperación automática del bot después de fallos.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public RecoverySettings Recovery { get; set; } = new();

    [Category(Integration), Description("Configuración del servidor del Panel de Control Web.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public WebServerSettings WebServer { get; set; } = new();
}
