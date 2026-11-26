# Publicar el Portal de Intercambios de TradeDex con Cloudflare Tunnel

Esta guía permite exponer el portal web de intercambios de TradeDex a otros usuarios sin abrir puertos del router ni compartir la dirección IP de la computadora.

Las páginas locales de TradeDex son:

```text
Panel de administración: http://localhost:8080/
Portal para usuarios:     http://localhost:8080/trade
```

Para una instalación pública, publica solamente el hostname del portal de intercambios y bloquea las rutas del panel administrativo.

## Elegir Una Configuración

| Configuración | URL pública | Uso recomendado |
| --- | --- | --- |
| Cloudflare Tunnel con dominio | `https://trade.ejemplo.com/trade` | Usuarios reales e inicio de sesión con Discord |
| Quick Tunnel sin dominio | URL aleatoria de `trycloudflare.com` | Pruebas locales breves solamente |

Un sitio público estable requiere un dominio agregado a Cloudflare. Un Quick Tunnel cambia de URL al reiniciarse y no es apropiado para un portal público con autenticación OAuth.

## Requisitos

- TradeDex/FusionBot ejecutándose en la PC Windows que alojará el sitio.
- Una cuenta de Cloudflare.
- Para la configuración recomendada: un dominio activo en la misma cuenta de Cloudflare.
- Para iniciar sesión con Discord: una aplicación en el Discord Developer Portal.

## 1. Activar el Servidor Web de TradeDex

En la aplicación TradeDex/FusionBot, abre **Web Server Settings** y configura:

```text
EnableWebServer = true
ControlPanelPort = 8080
AllowExternalConnections = false
```

Mantén `AllowExternalConnections` desactivado cuando `cloudflared` se ejecute en la misma computadora que TradeDex. El tunnel se conecta a `localhost`, por lo que la aplicación no necesita aceptar conexiones desde la red local ni desde Internet.

Reinicia la aplicación o aplica los ajustes. Después, prueba localmente en esa misma PC:

```text
http://localhost:8080/trade
```

No continúes hasta que el portal de intercambios cargue localmente.

## 2. Configuración Recomendada: Tunnel con Tu Dominio

### 2.1 Agregar el dominio a Cloudflare

1. Abre el panel de Cloudflare y entra a **Websites**.
2. Agrega tu dominio, por ejemplo `ejemplo.com`.
3. En el registrador donde compraste el dominio, reemplaza sus nameservers por los dos nameservers asignados por Cloudflare.
4. Espera hasta que Cloudflare muestre el dominio como **Active**.

Si el dominio no aparece como opción al crear la ruta de aplicación pública, todavía no está activo en la misma cuenta de Cloudflare.

### 2.2 Crear el tunnel

Los nombres de las opciones del panel pueden cambiar ligeramente, pero actualmente la ubicación es:

```text
Zero Trust > Networks > Connectors > Cloudflare Tunnels
```

1. Crea un Cloudflare Tunnel y asígnale un nombre, por ejemplo `tradedex`.
2. Elige el método de instalación del conector para **Windows**.
3. En la PC donde corre TradeDex, abre PowerShell como administrador y ejecuta el comando de instalación generado por Cloudflare.
4. Regresa a la página del tunnel y confirma que el estado del conector sea **Healthy**.

Importante: ejecuta `cloudflared` en la misma PC que TradeDex si la URL del servicio será `localhost:8080`. `localhost` siempre representa la computadora donde se está ejecutando el conector del tunnel.

### 2.3 Publicar el hostname para usuarios

Dentro del tunnel `tradedex`, abre:

```text
Published application routes
```

No uses **Hostname routes** para este sitio público. Esa sección corresponde a escenarios de rutas privadas o Cloudflare One Client.

Agrega una ruta de aplicación publicada:

```text
Subdomain: trade
Domain:    ejemplo.com
Path:      dejar vacío

Service type: HTTP
Service URL:  localhost:8080
```

El portal público quedará disponible en:

```text
https://trade.ejemplo.com/trade
```

No agregues `/trade` en la URL del servicio. Cloudflare debe reenviar el sitio completo hacia `http://localhost:8080`; la ruta del navegador es la que selecciona el portal de intercambios.

### 2.4 Solucionar `Bad Request - Invalid Hostname`

Si el tunnel muestra estado **Healthy**, pero el navegador presenta:

```text
Bad Request - Invalid Hostname
HTTP Error 400. The request hostname is invalid.
```

edita la ruta de aplicación publicada y abre sus opciones adicionales o de origin. Establece:

```text
HTTP Host Header: localhost:8080
```

Esto hace que la solicitud reenviada coincida con el listener HTTP local que TradeDex espera en Windows.

## 3. Configurar el Inicio de Sesión con Discord

Cuando el hostname público funcione, configura esta URL de callback en **ambos** lugares indicados abajo:

```text
https://trade.ejemplo.com/api/trade/auth/callback
```

En **Web Server Settings** de TradeDex/FusionBot:

```text
DiscordOAuthClientId     = el client ID de tu aplicación de Discord
DiscordOAuthClientSecret = el client secret de tu aplicación de Discord
DiscordOAuthRedirectUri  = https://trade.ejemplo.com/api/trade/auth/callback
```

En Discord Developer Portal:

```text
Your Application > OAuth2 > Redirects
```

Agrega exactamente la misma URL de callback:

```text
https://trade.ejemplo.com/api/trade/auth/callback
```

No agregues `/trade` a la URL de callback OAuth.

## 4. Redirigir la Raíz del Hostname al Portal de Intercambios

De forma predeterminada, la ruta local `/` corresponde al panel administrativo del bot, mientras que `/trade` corresponde al portal para usuarios. Crea una redirección para que las personas que escriban solo el hostname lleguen al portal de intercambios.

En la zona de Cloudflare de `ejemplo.com`, ve a:

```text
Rules > Redirect Rules > Create rule
```

Crea una redirección:

```text
Rule name: Redirect trade root to trade portal
```

Expresión personalizada:

```text
(http.host eq "trade.ejemplo.com" and http.request.uri.path eq "/")
```

Configuración de redirección:

```text
Type:        Static redirect
Destination: https://trade.ejemplo.com/trade
Status code: 302
```

Después de probar la configuración, puedes usar `301` para una redirección permanente. Durante la configuración inicial, `302` es más conveniente porque los navegadores la almacenan en caché con menor agresividad.

## 5. Bloquear las Rutas Administrativas en el Hostname Público

Una redirección solo facilita la navegación; no funciona como control de acceso. Un usuario aún podría solicitar rutas administrativas directamente si no se bloquean.

En la zona de Cloudflare de `ejemplo.com`, ve a:

```text
Security > WAF > Custom rules > Create rule
```

Crea una regla llamada:

```text
Block non-trade routes on public portal
```

Usa la siguiente expresión, reemplazando `trade.ejemplo.com` por tu hostname:

```text
(http.host eq "trade.ejemplo.com" and not (
    http.request.uri.path eq "/"
    or http.request.uri.path eq "/trade"
    or http.request.uri.path eq "/trade/"
    or http.request.uri.path eq "/TradePortal.css"
    or http.request.uri.path eq "/TradePortal.js"
    or http.request.uri.path eq "/icon.ico"
    or http.request.uri.path eq "/sv_mode_image.png"
    or http.request.uri.path eq "/plza_mode_image.png"
    or http.request.uri.path eq "/swsh_mode_image.png"
    or http.request.uri.path eq "/NormalPokeball.svg"
    or http.request.uri.path eq "/Pokeball1SVG.svg"
    or http.request.uri.path eq "/Pokeball2SVG.svg"
    or http.request.uri.path eq "/Pokeball3SVG.svg"
    or http.request.uri.path eq "/Pokeball4SVG.svg"
    or http.request.uri.path eq "/Pokeball5SVG.svg"
    or http.request.uri.path eq "/Pokeball6SVG.svg"
    or http.request.uri.path eq "/Pokeball7SVG.svg"
    or http.request.uri.path eq "/Pokeball8SVG.svg"
    or http.request.uri.path eq "/Pokeball9SVG.svg"
    or starts_with(http.request.uri.path, "/api/trade/")
    or http.request.uri.path eq "/api/bot/localization"
    or http.request.uri.path eq "/api/bot/instances"
))
```

Establece la acción en:

```text
Block
```

Actualmente, el Portal de Intercambios llama a `/api/bot/instances` para mostrar el modo de juego activo. Esa ruta debe estar permitida para que el portal renderice correctamente el modo actual. La ruta puede exponer información básica de las instancias del bot. Para una instalación más estricta, la aplicación debería exponer un endpoint público específico de modo/estado, y el portal debería dejar de consultar `/api/bot/instances`.

El hostname público ya no podrá usarse para administrar el bot. Usa el panel de control local:

```text
http://localhost:8080/
```

Si necesitas acceso administrativo remoto, publícalo en otro hostname protegido con autenticación de Cloudflare Access, nunca en el hostname público de usuarios.

## 6. Configuración Sin Dominio: Quick Tunnel Temporal

Usa este método solamente para pruebas. Un Quick Tunnel crea una URL pública aleatoria y temporal; no proporciona un dominio estable para tus usuarios.

1. Descarga `cloudflared` en la PC donde corre TradeDex.
2. Confirma que el portal local cargue:

   ```text
   http://localhost:8080/trade
   ```

3. Abre PowerShell en la carpeta que contiene `cloudflared.exe` y ejecuta:

   ```powershell
   .\cloudflared.exe tunnel --url http://localhost:8080 --http-host-header localhost:8080
   ```

4. `cloudflared` mostrará una URL temporal parecida a:

   ```text
   https://palabras-aleatorias.trycloudflare.com
   ```

5. Abre:

   ```text
   https://palabras-aleatorias.trycloudflare.com/trade
   ```

Limitaciones:

- La URL cambia cada vez que se reinicia el tunnel.
- La ruta raíz administrativa también puede quedar accesible, ya que no controlas reglas WAF ni Redirect Rules sobre el hostname temporal de Cloudflare.
- Para usar Discord OAuth, tendrías que cambiar la URL de callback en TradeDex y en Discord cada vez que cambie la URL temporal.

Por estas razones, no uses Quick Tunnels para ofrecer el portal de intercambios a usuarios reales.

## 7. Mantener el Tunnel Encendido

Para un tunnel con dominio instalado mediante el comando de conector Windows de Cloudflare, el conector normalmente se ejecuta como un servicio de Windows.

Desde PowerShell abierto como administrador:

```powershell
sc.exe query cloudflared
Start-Service cloudflared
Restart-Service cloudflared
```

En el panel de Cloudflare, el tunnel debe mostrar:

```text
Healthy
```

TradeDex también debe permanecer abierto con su servidor web activado. Si la aplicación se detiene, el tunnel puede seguir apareciendo como **Healthy**, pero los visitantes recibirán un error de gateway u origin.

## Solución de Problemas

### Cloudflare muestra `Bad Gateway`

Causa: el tunnel está conectado, pero no puede comunicarse con el servidor web de TradeDex.

Comprobaciones en la PC que ejecuta `cloudflared`:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8080/trade
netstat -ano | Select-String ":8080"
```

Verifica:

```text
EnableWebServer = true
ControlPanelPort = 8080
Service URL = localhost:8080
Service Type = HTTP
```

No configures el origin como `https://localhost:8080` salvo que hayas configurado HTTPS por separado en la aplicación local.

### Cloudflare muestra `Bad Request - Invalid Hostname`

El tunnel alcanza TradeDex, pero Windows rechaza el encabezado Host público. Configura lo siguiente en la ruta:

```text
HTTP Host Header: localhost:8080
```

### La página carga pero faltan imágenes o scripts

La regla WAF de bloqueo está denegando archivos estáticos necesarios. Comprueba que la lista de rutas permitidas incluya el JavaScript, CSS, icono, imágenes de modo y SVG de Pokeballs del Portal de Intercambios, tal como aparecen en la expresión WAF anterior.

### El inicio de sesión con Discord falla después de mover el portal a la URL pública

Comprueba que aparezca exactamente la misma URL en los ajustes de TradeDex y en Discord Developer Portal:

```text
https://trade.ejemplo.com/api/trade/auth/callback
```

### El dominio no aparece al agregar una ruta de aplicación publicada

El dominio debe estar agregado a la misma cuenta de Cloudflare y mostrar estado activo en **Websites** antes de que pueda seleccionarse en la ruta pública del tunnel.

## Enlaces de Referencia

- [Documentación de Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/)
- [Crear un tunnel desde el panel](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/get-started/create-remote-tunnel/)
- [Publicar aplicaciones con Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/routing-to-tunnel/)
- [Cloudflare Quick Tunnels](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/do-more-with-tunnels/trycloudflare/)
- [Cloudflare Redirect Rules](https://developers.cloudflare.com/rules/url-forwarding/single-redirects/)
- [Reglas personalizadas de Cloudflare WAF](https://developers.cloudflare.com/waf/custom-rules/)
- [Documentación de Discord OAuth2](https://discord.com/developers/docs/topics/oauth2)
