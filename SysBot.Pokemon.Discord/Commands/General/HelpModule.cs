using Discord;
using Discord.Commands;
using Discord.Net;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class HelpModule(CommandService commandService) : ModuleBase<SocketCommandContext>
{
#pragma warning disable CS9124
    private readonly CommandService _commandService = commandService;
#pragma warning restore CS9124

    private static readonly Color HelpColor = new(114, 137, 218);

    [Command("help")]
    [Summary("Shows the available commands.")]
    public async Task HelpAsync()
    {
        var prefix = SysCordSettings.HubConfig.Discord.CommandPrefix;
        var modules = await GetVisibleModulesAsync().ConfigureAwait(false);

        if (modules.Count == 0)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHelpNoCommandsAvailable)).ConfigureAwait(false);
            return;
        }

        var embeds = BuildHelpEmbeds(modules, prefix, Context.Client.CurrentUser);
        await SendHelpEmbedsAsync(embeds, AppLocalization.Format(LocalizationKeys.DiscordHelpDmSent, Context.User.Mention)).ConfigureAwait(false);
    }

    [Command("help")]
    [Summary("Shows the available commands.")]
    public Task HelpAsync(int page)
    {
        return HelpAsync();
    }

    [Command("help")]
    [Summary("Shows information about a specific command.")]
    public async Task HelpAsync([Summary("The command to get information for.")] string command)
    {
        var result = _commandService.Search(Context, command);
        if (!result.IsSuccess)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordHelpCommandNotFound, Context.User.Mention, command)).ConfigureAwait(false);
            return;
        }

        var prefix = SysCordSettings.HubConfig.Discord.CommandPrefix;
        var embed = BuildCommandHelpEmbed(result.Commands.Select(x => x.Command), command, prefix);
        await SendHelpEmbedsAsync([embed], AppLocalization.Format(LocalizationKeys.DiscordHelpCommandDmSent, Context.User.Mention, command)).ConfigureAwait(false);
    }

    private async Task<List<(string ModuleName, List<CommandInfo> Commands)>> GetVisibleModulesAsync()
    {
        var manager = SysCordSettings.Manager;
        var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
        var owner = app.Owner.Id;
        var userId = Context.User.Id;
        var modules = new List<(string ModuleName, List<CommandInfo> Commands)>();

        foreach (var module in _commandService.Modules)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var commands = new List<CommandInfo>();

            foreach (var command in module.Commands.OrderBy(x => x.Name))
            {
                if (!seen.Add(command.Name))
                    continue;

                if (command.Attributes.Any(a => a is RequireOwnerAttribute) && owner != userId)
                    continue;
                if (command.Attributes.Any(a => a is RequireSudoAttribute) && !manager.CanUseSudo(userId))
                    continue;

                var preconditions = await command.CheckPreconditionsAsync(Context).ConfigureAwait(false);
                if (preconditions.IsSuccess)
                    commands.Add(command);
            }

            if (commands.Count == 0)
                continue;

            modules.Add((GetFriendlyModuleName(module.Name), commands));
        }

        return modules
            .OrderByDescending(x => IsTradeModule(x.ModuleName))
            .ThenBy(x => x.ModuleName)
            .ToList();
    }

    private static IReadOnlyList<Embed> BuildHelpEmbeds(
        List<(string ModuleName, List<CommandInfo> Commands)> modules,
        string prefix,
        IUser botUser)
    {
        var embeds = new List<Embed>();
        var builder = MakeBaseHelpEmbed(prefix, botUser);
        var inlineColumn = 0;

        foreach (var (module, commands) in modules)
        {
            var chunks = ChunkCommandLines(commands, prefix);
            for (int i = 0; i < chunks.Count; i++)
            {
                if (!EnsureCapacityOrNew(ref builder, embeds, prefix, botUser, 1))
                    inlineColumn = 0;

                var moduleName = chunks.Count == 1 ? module : $"{module} {i + 1}/{chunks.Count}";
                builder.AddField(AppLocalization.Format(LocalizationKeys.DiscordHelpModuleField, moduleName), chunks[i], true);
                inlineColumn++;

                if (inlineColumn == 2)
                {
                    if (EnsureCapacityOrNew(ref builder, embeds, prefix, botUser, 1))
                        builder.AddField("\u200B", "\u200B", false);

                    inlineColumn = 0;
                }
            }
        }

        if (builder.Fields.Count > 0)
            embeds.Add(builder.Build());

        for (int i = 0; i < embeds.Count; i++)
        {
            var built = embeds[i];
            var pageFooter = AppLocalization.Format(LocalizationKeys.DiscordHelpFooterPage, i + 1, embeds.Count);
            var footer = $"{AppLocalization.Format(LocalizationKeys.DiscordHelpTipFooter, prefix)} | {pageFooter}";

            embeds[i] = built.ToEmbedBuilder()
                .WithFooter(footer, built.Footer?.IconUrl)
                .Build();
        }

        return embeds;
    }

    private static bool EnsureCapacityOrNew(ref EmbedBuilder builder, List<Embed> embeds, string prefix, IUser botUser, int neededSlots)
    {
        const int MaxFields = 25;
        if (builder.Fields.Count + neededSlots <= MaxFields)
            return true;

        embeds.Add(builder.Build());
        builder = MakeBaseHelpEmbed(prefix, botUser);
        return false;
    }

    private Embed BuildCommandHelpEmbed(IEnumerable<CommandInfo> commands, string searchedCommand, string prefix)
    {
        var embed = new EmbedBuilder()
            .WithColor(HelpColor)
            .WithAuthor(AppLocalization.Get(LocalizationKeys.DiscordHelpCommandAuthor), Context.Client.CurrentUser.GetAvatarUrl() ?? Context.Client.CurrentUser.GetDefaultAvatarUrl())
            .WithTitle($"{prefix}{searchedCommand}")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl() ?? Context.Client.CurrentUser.GetDefaultAvatarUrl())
            .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordHelpCommandFooter, prefix))
            .WithCurrentTimestamp();

        foreach (var command in commands.OrderBy(x => x.Name))
        {
            var summary = AppLocalization.GetCommandSummary(command.Summary);
            var parameters = command.Parameters.Count == 0
                ? $"_{AppLocalization.Get(LocalizationKeys.DiscordHelpNoParameters)}_"
                : string.Join("\n", command.Parameters.Select(FormatParameter));

            embed.AddField(
                AppLocalization.Format(LocalizationKeys.DiscordHelpCommandField, command.Name),
                $"{summary}\n\n**{AppLocalization.Get(LocalizationKeys.DiscordHelpParametersLabel)}:**\n{parameters}\n\n**{AppLocalization.Get(LocalizationKeys.DiscordHelpExampleLabel)}:**\n`{BuildExample(command, prefix)}`",
                false);
        }

        return embed.Build();
    }

    private static List<string> ChunkCommandLines(List<CommandInfo> commands, string prefix)
    {
        const int MaxFieldLength = 950;
        var chunks = new List<string>();
        var lines = commands.Select(command =>
        {
            var alias = command.Aliases.FirstOrDefault() ?? command.Name;
            var aliases = command.Aliases
                .Where(x => !x.Equals(alias, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .Select(x => $"`{prefix}{x}`");
            var aliasText = string.Join(" ", aliases);
            return string.IsNullOrWhiteSpace(aliasText)
                ? $"• `{prefix}{alias}`"
                : $"• `{prefix}{alias}` {aliasText}";
        });

        var current = new List<string>();
        var currentLength = 0;
        foreach (var line in lines)
        {
            if (current.Count > 0 && currentLength + line.Length + 1 > MaxFieldLength)
            {
                chunks.Add(string.Join("\n", current));
                current.Clear();
                currentLength = 0;
            }

            current.Add(line);
            currentLength += line.Length + 1;
        }

        if (current.Count > 0)
            chunks.Add(string.Join("\n", current));

        return chunks;
    }

    private async Task SendHelpEmbedsAsync(IReadOnlyList<Embed> embeds, string sentNotice)
    {
        try
        {
            var dm = await Context.User.CreateDMChannelAsync().ConfigureAwait(false);
            foreach (var embed in embeds)
                await dm.SendMessageAsync(embed: embed).ConfigureAwait(false);

            if (Context.Channel is IGuildChannel)
            {
                await TryDeleteAsync(Context.Message).ConfigureAwait(false);
                var notice = await ReplyAsync(sentNotice).ConfigureAwait(false);
                _ = DeleteAfterDelayAsync(notice, 10);
            }
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
        {
            foreach (var embed in embeds)
                await ReplyAsync(embed: embed).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordHelpDmError, ex.Message)).ConfigureAwait(false);
        }
    }

    private static EmbedBuilder MakeBaseHelpEmbed(string prefix, IUser botUser)
    {
        return new EmbedBuilder()
            .WithColor(HelpColor)
            .WithAuthor(AppLocalization.Get(LocalizationKeys.DiscordHelpCenterTitle), botUser.GetAvatarUrl() ?? botUser.GetDefaultAvatarUrl())
            .WithDescription(AppLocalization.Get(LocalizationKeys.DiscordHelpDescription))
            .WithThumbnailUrl(botUser.GetAvatarUrl() ?? botUser.GetDefaultAvatarUrl())
            .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordHelpTipFooter, prefix))
            .WithCurrentTimestamp();
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        var optional = parameter.IsOptional ? $" {AppLocalization.Get(LocalizationKeys.DiscordHelpOptionalParameter)}" : string.Empty;
        var summary = AppLocalization.GetCommandSummary(parameter.Summary);
        return $"• `{AppLocalization.GetCommandSummary(parameter.Name)}`{optional} - {summary}";
    }

    private static string BuildExample(CommandInfo command, string prefix)
    {
        var alias = command.Aliases.FirstOrDefault() ?? command.Name;
        var args = string.Join(" ", command.Parameters.Select(p =>
        {
            var name = AppLocalization.GetCommandSummary(p.Name);
            return p.IsOptional ? $"[{name}]" : $"<{name}>";
        }));
        return $"{prefix}{alias} {args}".TrimEnd();
    }

    private static string GetFriendlyModuleName(string moduleName)
    {
        var clean = moduleName.Split('`')[0];
        return clean.EndsWith("Module", StringComparison.Ordinal) ? clean[..^"Module".Length] : clean;
    }

    private static bool IsTradeModule(string moduleName) =>
        moduleName.Contains("Trade", StringComparison.OrdinalIgnoreCase) ||
        moduleName.Contains("Queue", StringComparison.OrdinalIgnoreCase);

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
            // Missing permissions or already-deleted messages should not break help.
        }
    }
}
