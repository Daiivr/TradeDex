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
    private const int MaxComponentTextLength = 3900;
    private const int MaxHelpContainerTextLength = 3600;
    private const int MaxHelpBlocksPerContainer = 4;

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

        var pages = BuildHelpComponents(modules, prefix, Context.Client.CurrentUser);
        await SendHelpComponentsAsync(pages, AppLocalization.Format(LocalizationKeys.DiscordHelpDmSent, Context.User.Mention)).ConfigureAwait(false);
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
        var page = BuildCommandHelpComponent(result.Commands.Select(x => x.Command), command, prefix);
        await SendHelpComponentsAsync([page], AppLocalization.Format(LocalizationKeys.DiscordHelpCommandDmSent, Context.User.Mention, command)).ConfigureAwait(false);
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
            .OrderBy(x => GetHelpCategory(x.ModuleName).Order)
            .ThenBy(x => x.ModuleName)
            .ToList();
    }

    private static IReadOnlyList<MessageComponent> BuildHelpComponents(
        List<(string ModuleName, List<CommandInfo> Commands)> modules,
        string prefix,
        IUser botUser)
    {
        var blocks = BuildCategorizedHelpBlocks(modules, prefix);

        var sections = BuildHelpBlockGroups(blocks);
        var avatarUrl = GetUserAvatarUrl(botUser);
        var footer = AppLocalization.Format(LocalizationKeys.DiscordHelpTipFooter, prefix);

        return
        [
            BuildHelpMessageComponent(
                AppLocalization.Get(LocalizationKeys.DiscordHelpCenterTitle),
                AppLocalization.Get(LocalizationKeys.DiscordHelpDescription),
                sections,
                footer,
                avatarUrl,
                botUser.Username)
        ];
    }

    private static List<string> BuildCategorizedHelpBlocks(
        List<(string ModuleName, List<CommandInfo> Commands)> modules,
        string prefix)
    {
        var blocks = new List<string>();
        var groups = modules
            .Select(x => (Category: GetHelpCategory(x.ModuleName), x.ModuleName, x.Commands))
            .OrderBy(x => x.Category.Order)
            .ThenBy(x => x.ModuleName)
            .GroupBy(x => x.Category);

        foreach (var group in groups)
        {
            var categoryBlocks = new List<string>();

            foreach (var (_, module, commands) in group)
            {
                var chunks = ChunkCommandLines(commands, prefix);
                for (int i = 0; i < chunks.Count; i++)
                {
                    var moduleName = chunks.Count == 1 ? module : $"{module} {i + 1}/{chunks.Count}";
                    categoryBlocks.Add($"**{AppLocalization.Format(LocalizationKeys.DiscordHelpModuleField, moduleName)}**\n{chunks[i]}");
                }
            }

            blocks.AddRange(BuildCategoryBlocks(group.Key.Title, categoryBlocks));
        }

        return blocks;
    }

    private static List<string> BuildCategoryBlocks(string categoryTitle, IReadOnlyList<string> moduleBlocks)
    {
        var blocks = new List<string>();
        var header = $"**{categoryTitle}:**";
        var current = header;

        foreach (var moduleBlock in moduleBlocks)
        {
            var separator = current.Equals(header, StringComparison.Ordinal) ? "\n" : "\n\n";
            if (!current.Equals(header, StringComparison.Ordinal) &&
                current.Length + separator.Length + moduleBlock.Length > MaxHelpContainerTextLength)
            {
                blocks.Add(current);
                current = $"**{categoryTitle} (continued):**";
                separator = "\n";
            }

            current = $"{current}{separator}{moduleBlock}";
        }

        if (!string.IsNullOrWhiteSpace(current))
            blocks.Add(current);

        return blocks;
    }

    private static List<List<string>> BuildHelpBlockGroups(IReadOnlyList<string> blocks)
    {
        var groups = new List<List<string>>();
        var current = new List<string>();
        var currentLength = 0;

        foreach (var block in blocks)
        {
            var nextLength = currentLength + block.Length;
            if (current.Count > 0 && (nextLength > MaxHelpContainerTextLength || current.Count >= MaxHelpBlocksPerContainer))
            {
                groups.Add(current);
                current = [];
                currentLength = 0;
            }

            current.Add(block);
            currentLength += block.Length;
        }

        if (current.Count > 0)
            groups.Add(current);

        return groups;
    }

    private MessageComponent BuildCommandHelpComponent(IEnumerable<CommandInfo> commands, string searchedCommand, string prefix)
    {
        var blocks = new List<string>();

        foreach (var command in commands.OrderBy(x => x.Name))
        {
            var summary = AppLocalization.GetCommandSummary(command.Summary);
            var parameters = command.Parameters.Count == 0
                ? $"_{AppLocalization.Get(LocalizationKeys.DiscordHelpNoParameters)}_"
                : string.Join("\n", command.Parameters.Select(FormatParameter));

            blocks.Add(
                $"**{AppLocalization.Format(LocalizationKeys.DiscordHelpCommandField, command.Name)}**\n" +
                $"{summary}\n\n" +
                $"**{AppLocalization.Get(LocalizationKeys.DiscordHelpParametersLabel)}:**\n{parameters}\n\n" +
                $"**{AppLocalization.Get(LocalizationKeys.DiscordHelpExampleLabel)}:**\n`{BuildExample(command, prefix)}`");
        }

        return BuildHelpMessageComponent(
            AppLocalization.Get(LocalizationKeys.DiscordHelpCommandAuthor),
            $"**{prefix}{searchedCommand}**",
            BuildHelpBlockGroups(blocks),
            AppLocalization.Format(LocalizationKeys.DiscordHelpCommandFooter, prefix),
            Context.Client.CurrentUser.GetAvatarUrl() ?? Context.Client.CurrentUser.GetDefaultAvatarUrl(),
            Context.Client.CurrentUser.Username);
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

    private async Task SendHelpComponentsAsync(IReadOnlyList<MessageComponent> pages, string sentNotice)
    {
        try
        {
            var dm = await Context.User.CreateDMChannelAsync().ConfigureAwait(false);
            foreach (var page in pages)
                await dm.SendMessageAsync(components: page, flags: MessageFlags.ComponentsV2).ConfigureAwait(false);

            if (Context.Channel is IGuildChannel)
            {
                await TryDeleteAsync(Context.Message).ConfigureAwait(false);
                var notice = await ReplyAsync(sentNotice).ConfigureAwait(false);
                _ = DeleteAfterDelayAsync(notice, 10);
            }
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
        {
            foreach (var page in pages)
            {
                await Context.Channel.SendMessageAsync(
                    components: page,
                    flags: MessageFlags.ComponentsV2).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordHelpDmError, ex.Message)).ConfigureAwait(false);
        }
    }

    private static MessageComponent BuildHelpMessageComponent(
        string title,
        string description,
        IReadOnlyList<List<string>> blocks,
        string footer,
        string avatarUrl,
        string avatarDescription)
    {
        var builder = new ComponentBuilderV2();

        for (int i = 0; i < blocks.Count; i++)
        {
            var isFirst = i == 0;
            var pageTitle = isFirst || blocks.Count == 1
                ? title
                : $"{title} {i + 1}/{blocks.Count}";
            var pageDescription = isFirst
                ? description
                : AppLocalization.Format(LocalizationKeys.DiscordHelpFooterPage, i + 1, blocks.Count);
            var pageFooter = blocks.Count == 1
                ? footer
                : $"{footer} | {AppLocalization.Format(LocalizationKeys.DiscordHelpFooterPage, i + 1, blocks.Count)}";

            builder.WithContainer(BuildHelpContainer(
                pageTitle,
                pageDescription,
                blocks[i],
                pageFooter,
                isFirst ? avatarUrl : string.Empty,
                avatarDescription));
        }

        return builder.Build();
    }

    private static ContainerBuilder BuildHelpContainer(
        string title,
        string description,
        IReadOnlyList<string> bodyBlocks,
        string footer,
        string avatarUrl,
        string avatarDescription)
    {
        var container = new ContainerBuilder()
            .WithAccentColor(HelpColor);

        var headerText = TrimComponentText($"**{title}**\n{description}");
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            container.WithTextDisplay(headerText);
        }
        else
        {
            var header = new SectionBuilder()
                .AddComponent(new TextDisplayBuilder(headerText))
                .WithAccessory(new ThumbnailBuilder(
                    new UnfurledMediaItemProperties(avatarUrl),
                    avatarDescription,
                    false));

            container.WithSection(header);
        }

        foreach (var body in bodyBlocks)
        {
            container.WithSeparator(SeparatorSpacingSize.Small, true);
            container.WithTextDisplay(TrimComponentText(body));
        }

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText(footer));

        return container;
    }

    private static string GetUserAvatarUrl(IUser user) =>
        user.GetAvatarUrl(size: 64) ?? user.GetDefaultAvatarUrl();

    private static string TrimComponentText(string text, int maxLength = MaxComponentTextLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "\u200B";

        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
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

    private static (int Order, string Title) GetHelpCategory(string moduleName)
    {
        var normalized = moduleName.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (MatchesAny(normalized, "Trade", "Clone", "Dump", "MysteryEgg", "MysteryMon", "HOMEReady", "SpecialRequest", "Pokepaste"))
            return (0, "Trade Commands");

        if (MatchesAny(normalized, "Queue"))
            return (1, "Queue Commands");

        if (MatchesAny(normalized, "Legalizer", "LegalityCheck", "BatchEditing", "SeedCheck", "Smogon"))
            return (2, "Pokemon Tools");

        if (MatchesAny(normalized, "Help", "Hello", "Ping", "Info", "Profile", "Stream", "Donate", "Tutorial", "Joke"))
            return (3, "General Commands");

        if (MatchesAny(normalized, "Hub", "Bot", "TradeStart", "Log", "Recovery", "Pool", "RemoteControl", "Echo", "Encounter"))
            return (4, "Bot Management");

        if (MatchesAny(normalized, "Owner", "Sudo", "PKHeX", "BotAvatar"))
            return (5, "Owner & Moderation");

        return (6, "Other Commands");
    }

    private static bool MatchesAny(string value, params string[] names) =>
        names.Any(name => value.Contains(name, StringComparison.OrdinalIgnoreCase));

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
