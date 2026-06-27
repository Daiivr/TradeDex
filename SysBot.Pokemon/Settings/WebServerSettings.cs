using System.ComponentModel;
using SysBot.Pokemon.Localization;

namespace SysBot.Pokemon;

/// <summary>
/// Settings for the Web Control Panel server
/// </summary>
public sealed class WebServerSettings
{
    private const string WebServer = nameof(WebServer);

    [Category(WebServer)]
    [Description("El número de puerto para la interfaz web del Panel de Control del Bot. Por defecto es 8080.")]
    public int ControlPanelPort { get; set; } = 8080;

    [Category(WebServer)]
    [Description("Habilitar o deshabilitar el panel de control web. Cuando está deshabilitado, la interfaz web no será accesible.")]
    public bool EnableWebServer { get; set; } = false;

    [Category(WebServer)]
    [Description("Permitir conexiones externas al panel de control web. Cuando es falso, solo se permiten conexiones desde localhost.")]
    public bool AllowExternalConnections { get; set; } = false;

    [Category(WebServer)]
    [Description("Client ID de la aplicación de Discord usada para el login web.")]
    public string DiscordOAuthClientId { get; set; } = string.Empty;

    [Category(WebServer)]
    [Description("Client Secret de la aplicación de Discord usada para el login web.")]
    public string DiscordOAuthClientSecret { get; set; } = string.Empty;

    [Category(WebServer)]
    [Description("URL de callback OAuth de Discord. Si se deja vacía, se usará http://localhost:{puerto}/api/trade/auth/callback.")]
    public string DiscordOAuthRedirectUri { get; set; } = string.Empty;

    [Category(WebServer)]
    [DisplayName("Admin ID")]
    [Description("Discord ID del usuario que puede ver el botón Control en la página de trades web. Si se deja vacío, el botón Control no se mostrará a usuarios logueados.")]
    public string AdminID { get; set; } = string.Empty;
}
