using System.ComponentModel;
using System.IO;
using SysBot.Pokemon.Localization;

namespace SysBot.Pokemon;

public class FolderSettings : IDumper
{
    private const string FeatureToggle = nameof(FeatureToggle);

    private const string Files = nameof(Files);

    [Category(Files), Description("Carpeta de origen: desde donde se seleccionan los archivos PKM a distribuir."), DisplayName("Carpeta de Distribución")]
    public string DistributeFolder { get; set; } = string.Empty;

    [Category(FeatureToggle), Description("Cuando está habilitado, vuelca todos los archivos PKM recibidos (resultados comerciales) en la carpeta de volcado."), DisplayName("Habilitar el Volcado de Archivos (Dump)")]
    public bool Dump { get; set; }

    [Category(Files), Description("Carpeta de destino: donde se descargan todos los archivos PKM recibidos."), DisplayName("Carpeta de Volcado (Dump)")]
    public string DumpFolder { get; set; } = string.Empty;

    [Category(Files), Description("Directorio donde se encuentran tus Pokemon con HOME Tracker."), DisplayName("Carpeta HOME-Ready")]
    public string HOMEReadyPKMFolder { get; set; } = string.Empty;

    [Category(Files), Description("Path to your Events Folder. Create a new folder called 'events' and copy the path here."), DisplayName("Events Folder")]
    public string EventsFolder { get; set; } = string.Empty;

    [Category(Files), Description("Path to your BattleReady Folder. Create a new folder called 'battleready' and copy the path here."), DisplayName("Battle-Ready Folder")]
    public string BattleReadyPKMFolder { get; set; } = string.Empty;

    [Category(Files), Description("Directorio donde se encuentra tu ejecutable de PKHeX."), DisplayName("Carpeta de PKHeX")]
    public string PKHeXDirectory { get; set; } = string.Empty;

    [Category(Files), Description("Directorio donde se encuentra Switch Remote For PC."), DisplayName("Ubicacion de Switch Remote for PC")]
    public string SwitchRemoteForPC { get; set; } = string.Empty;

    public void CreateDefaults(string path)
    {
        var dump = Path.Combine(path, "dump");
        Directory.CreateDirectory(dump);
        DumpFolder = dump;
        Dump = true;

        var distribute = Path.Combine(path, "distribute");
        Directory.CreateDirectory(distribute);
        DistributeFolder = distribute;
    }

    public override string ToString() => "Configuración de carpeta/dump";
}
