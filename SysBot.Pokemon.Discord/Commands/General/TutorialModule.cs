using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Pokemon.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class TutorialModule : ModuleBase<SocketCommandContext>
{
    private const string IconUrl = "https://i.imgur.com/axXN5Sd.gif";
    private const string ThumbnailUrl = "https://i.imgur.com/lPU9wFp.png";
    private static readonly Color TutorialColor = new(88, 101, 242);

    [Command("ayuda")]
    [Alias("tutorial", "tutoriales")]
    [Summary("Muestra una guia interactiva para los comandos mas usados.")]
    public async Task TutorialAsync([Remainder] string? command = null)
    {
        var prefix = SysCordSettings.HubConfig.Discord.CommandPrefix;

        if (!string.IsNullOrWhiteSpace(command))
        {
            var embed = BuildTutorialEmbed(command.Trim(), prefix).Build();
            await SendTutorialDmAsync(embed, command.Trim()).ConfigureAwait(false);
            return;
        }

        var builder = new EmbedBuilder()
            .WithColor(TutorialColor)
            .WithTitle("Comandos disponibles")
            .WithDescription($"Selecciona un tema del menu para ver como usarlo.\n\nTambien puedes usar `{prefix}ayuda <tema>` para abrirlo directamente.")
            .AddField("Temas", BuildTopicList(prefix), false)
            .WithThumbnailUrl(ThumbnailUrl)
            .WithFooter("El menu se cerrara automaticamente despues de 2 minutos.")
            .WithCurrentTimestamp();

        var selectMenu = new SelectMenuBuilder()
            .WithPlaceholder("Selecciona un tutorial...")
            .WithCustomId($"tutorial_menu:{Context.User.Id}")
            .AddOption("Pedidos Especiales", "sr", "Modificar un Pokemon con objeto o apodo")
            .AddOption("Intercambio por Lotes", "bt", "Enviar varios sets en un solo proceso")
            .AddOption("Clone", "clone", "Clonar un Pokemon")
            .AddOption("Fix", "fix", "Limpiar apodos publicitarios")
            .AddOption("Ditto", "ditto", "Pedir Dittos para crianza")
            .AddOption("Huevo Misterioso", "me", "Pedir un huevo misterioso")
            .AddOption("Huevos", "egg", "Pedir huevos especificos")
            .AddOption("Eventos", "le", "Listar y pedir eventos")
            .AddOption("Regalos Misteriosos", "srp", "Pedir wondercards/eventos")
            .AddOption("Item Trade", "item", "Pedir items")
            .AddOption("Codigos de Intercambio", "codigos", "Guardar, actualizar o borrar tu codigo");

        var closeButton = new ButtonBuilder()
            .WithLabel("Cerrar")
            .WithStyle(ButtonStyle.Danger)
            .WithCustomId($"tutorial_close:{Context.User.Id}");

        var components = new ComponentBuilder()
            .WithSelectMenu(selectMenu)
            .WithButton(closeButton)
            .Build();

        var message = await ReplyAsync(embed: builder.Build(), components: components).ConfigureAwait(false);
        await TryDeleteAsync(Context.Message).ConfigureAwait(false);
        await HandleInteractionsAsync(message, prefix).ConfigureAwait(false);
    }

    private async Task SendTutorialDmAsync(Embed embed, string command)
    {
        try
        {
            var dm = await Context.User.CreateDMChannelAsync().ConfigureAwait(false);
            await dm.SendMessageAsync(embed: embed).ConfigureAwait(false);

            if (Context.Channel is IGuildChannel)
            {
                await TryDeleteAsync(Context.Message).ConfigureAwait(false);
                var confirmation = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordTutorialDmSent, Context.User.Mention, command)).ConfigureAwait(false);
                _ = DeleteAfterDelayAsync(confirmation, 7);
            }
        }
        catch
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordTutorialDmFailed, Context.User.Mention, SysCordSettings.HubConfig.Discord.CommandPrefix)).ConfigureAwait(false);
        }
    }

    private async Task HandleInteractionsAsync(IUserMessage message, string prefix)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        while (!cancellation.IsCancellationRequested)
        {
            var interaction = await WaitForInteractionAsync(message.Id, Context.User.Id, cancellation.Token).ConfigureAwait(false);
            if (interaction == null)
                break;

            if (interaction.Data.CustomId.StartsWith("tutorial_close:", StringComparison.Ordinal))
            {
                await interaction.Message.DeleteAsync().ConfigureAwait(false);
                return;
            }

            await interaction.DeferAsync().ConfigureAwait(false);
            var topic = interaction.Data.Values.FirstOrDefault() ?? string.Empty;
            await message.ModifyAsync(x => x.Embed = BuildTutorialEmbed(topic, prefix).Build()).ConfigureAwait(false);
        }

        await message.ModifyAsync(x => x.Components = new ComponentBuilder().Build()).ConfigureAwait(false);
    }

    private async Task<SocketMessageComponent?> WaitForInteractionAsync(ulong messageId, ulong userId, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<SocketMessageComponent?>();

        Task Handler(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component &&
                component.Message.Id == messageId &&
                component.User.Id == userId)
            {
                tcs.TrySetResult(component);
            }

            return Task.CompletedTask;
        }

        Context.Client.InteractionCreated += Handler;
        try
        {
            await using (token.Register(() => tcs.TrySetResult(null)).ConfigureAwait(false))
                return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            Context.Client.InteractionCreated -= Handler;
        }
    }

    private static EmbedBuilder BuildTutorialEmbed(string topic, string prefix)
    {
        var builder = new EmbedBuilder()
            .WithColor(TutorialColor)
            .WithThumbnailUrl(ThumbnailUrl)
            .WithCurrentTimestamp();

        switch (topic.ToLowerInvariant())
        {
            case "sr":
                return builder.WithAuthor("Pedidos Especiales", IconUrl)
                    .WithDescription($"# Pedidos Especiales\nUsa `{prefix}sr` para modificar un Pokemon que tu muestras al bot.\n\n**Flujo:**\n1. Dale al Pokemon el objeto o apodo que representa el cambio.\n2. Usa `{prefix}sr`.\n3. Muestra el Pokemon al bot.\n4. Cambialo por un descarte cuando el bot lo pida.\n\n**Ejemplos de efectos:**\n- Poké Ball: borra apodo.\n- Great Ball: reemplaza OT.\n- Ultra Ball: borra OT y apodo.\n- Antidote/Antiquemar: shiny, si es legal.\n- Mentas: cambian naturaleza.");

            case "bt":
                return builder.WithAuthor("Intercambio por Lotes", IconUrl)
                    .WithDescription($"# Intercambio por Lotes\nUsa `{prefix}bt` para pedir varios Pokemon en un mismo proceso.\n\n**Formato:**\n```{prefix}bt\nGreninja @ Life Orb\nLevel: 100\n---\nCharizard @ Ability Patch\nLevel: 100\n---\nPichu @ Light Ball\nLevel: 5```\n\nSepara cada set con `---`. El bot abrira y cerrara los trades segun avance el lote.");

            case "clone":
                return builder.WithAuthor("Clonar Pokemon", IconUrl)
                    .WithDescription($"# Clone\nUsa `{prefix}clone` para clonar el Pokemon que muestres.\n\n**Flujo:**\n1. El bot te da un codigo.\n2. Muestra el Pokemon que quieres clonar.\n3. Cancela esa oferta cuando el bot lo indique.\n4. Entrega un descarte para recibir la copia.");

            case "fix":
                return builder.WithAuthor("Fix", IconUrl)
                    .WithDescription($"# Fix\nUsa `{prefix}fix` para limpiar apodos publicitarios o nombres no deseados.\n\nEl bot clona el Pokemon, limpia el apodo cuando sea posible y te devuelve una copia corregida.");

            case "ditto":
                return builder.WithAuthor("Ditto", IconUrl)
                    .WithDescription($"# Ditto\nUsa `{prefix}ditto <modificador> <idioma> <naturaleza>`.\n\n**Ejemplos:**\n```{prefix}ditto ATKSPE Japanese Modest\n{prefix}ditto 6IV Korean Timid```\n\n**Modificadores comunes:** `ATK`, `SPE`, `SPA`, `ATKSPE`, `ATKSPESPA`, `6IV`.");

            case "me":
                return builder.WithAuthor("Huevo Misterioso", IconUrl)
                    .WithDescription($"# Huevo Misterioso\nUsa `{prefix}me` para pedir un huevo aleatorio.\n\nEl contenido es sorpresa y se genera automaticamente. Si el servidor permite lotes, tambien puede haber comandos de batch para huevos misteriosos.");

            case "egg":
                return builder.WithAuthor("Huevos", IconUrl)
                    .WithDescription($"# Huevos\nUsa `{prefix}egg <pokemon o set>` para pedir un huevo especifico.\n\n**Ejemplo:**\n```{prefix}egg Charmander\nShiny: Yes\nNature: Timid```\n\nLos datos del set se aplican al Pokemon que saldra del huevo cuando sean legales.");

            case "le":
                return builder.WithAuthor("Eventos", IconUrl)
                    .WithDescription($"# Eventos\nUsa `{prefix}le [filtro] [pagina]` para listar eventos disponibles.\n\n**Ejemplos:**\n```{prefix}le pikachu\n{prefix}le d 2```\n\nLa lista te indicara el comando o indice que debes usar para solicitar el evento.");

            case "srp":
                return builder.WithAuthor("Regalos Misteriosos", IconUrl)
                    .WithDescription($"# Regalos Misteriosos\nUsa `{prefix}srp <juego> [pagina|indice]` para listar o pedir wondercards.\n\n**Ejemplos:**\n```{prefix}srp gen9\n{prefix}srp swsh page2\n{prefix}srp gen9 10```\n\nPuedes pedir eventos de otros juegos y el bot intentara legalizarlos para el juego activo.");

            case "item":
                return builder.WithAuthor("Item Trade", IconUrl)
                    .WithDescription($"# Item Trade\nUsa `{prefix}item <item>` o `{prefix}it <item>` para recibir el item solicitado.\n\n**Ejemplo:**\n```{prefix}item Armor Ball```\n\nPara lotes de items usa `{prefix}ibt <item> <cantidad>` si esta habilitado.");

            case "codigos":
                return builder.WithAuthor("Codigos de Intercambio", IconUrl)
                    .WithDescription($"# Codigos de Intercambio\nGuarda un codigo fijo para que el bot lo use en tus trades.\n\n**Comandos:**\n- `{prefix}atc <codigo>`: guarda codigo.\n- `{prefix}utc <codigo>`: actualiza codigo.\n- `{prefix}dtc`: elimina codigo.\n\nLos codigos deben tener 8 digitos.");

            default:
                return builder.WithAuthor("Tutorial no encontrado", IconUrl)
                    .WithDescription($"No encontre un tutorial para `{topic}`.\n\nUsa `{prefix}ayuda` para ver la lista de temas.");
        }
    }

    private static string BuildTopicList(string prefix) =>
        $"- `{prefix}ayuda sr` - Pedidos Especiales\n" +
        $"- `{prefix}ayuda bt` - Intercambio por Lotes\n" +
        $"- `{prefix}ayuda clone` - Clonar Pokemon\n" +
        $"- `{prefix}ayuda fix` - Limpiar apodos\n" +
        $"- `{prefix}ayuda ditto` - Dittos\n" +
        $"- `{prefix}ayuda me` - Huevo Misterioso\n" +
        $"- `{prefix}ayuda egg` - Huevos\n" +
        $"- `{prefix}ayuda le` - Eventos\n" +
        $"- `{prefix}ayuda srp` - Regalos Misteriosos\n" +
        $"- `{prefix}ayuda item` - Item Trade\n" +
        $"- `{prefix}ayuda codigos` - Codigos de Intercambio";

    private static async Task DeleteAfterDelayAsync(IMessage message, int seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds)).ConfigureAwait(false);
        await TryDeleteAsync(message).ConfigureAwait(false);
    }

    private static async Task TryDeleteAsync(IMessage message)
    {
        try
        {
            await message.DeleteAsync().ConfigureAwait(false);
        }
        catch
        {
            // Missing permissions or already-deleted messages should not break tutorials.
        }
    }
}
