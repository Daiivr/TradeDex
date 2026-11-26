using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class HubModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private const int MaxComponentTextLength = 3900;

    [Command("status")]
    [Alias("stats")]
    [Summary("Gets the status of the bot environment.")]
    public async Task GetStatusAsync()
    {
        var runner = SysCord<T>.Runner;
        var hub = runner.Hub;
        var allBots = runner.Bots.ConvertAll(z => z.Bot);
        var botCount = allBots.Count;

        var noBots = botCount == 0;
        var queues = hub.Queues.AllQueues;
        var queuesEmpty = queues.All(q => q.Count == 0);
        var (statusEmoji, statusLabel, statusColor) = GetGlobalStatus(noBots, queuesEmpty);

        var botsSummary = SummarizeBotsWithBadges(allBots);
        var botsSection = string.IsNullOrWhiteSpace(botsSummary)
            ? string.Empty
            : $"\n```{Environment.NewLine}{botsSummary}{Environment.NewLine}```";

        var summary = AppLocalization.Format(LocalizationKeys.DiscordHubSummaryValue, botCount, botsSection, statusLabel, hub.Ledy.Pool.Count);

        var bots = allBots.OfType<ICountBot>();
        var lines = bots.SelectMany(z => z.Counts.GetNonZeroCounts()).Distinct();
        var counts = string.Join("\n", lines);
        if (string.IsNullOrWhiteSpace(counts))
            counts = AppLocalization.Get(LocalizationKeys.DiscordHubNothingCounted);

        var totalQueued = queues.Sum(q => q.Count);
        var queueSections = new List<(string Title, string Body)>();
        if (totalQueued == 0)
        {
            queueSections.Add((
                AppLocalization.Get(LocalizationKeys.DiscordHubQueuesEmptyTitle),
                AppLocalization.Get(LocalizationKeys.DiscordHubQueuesEmptyValue)));
        }
        else
        {
            queueSections.Add((
                AppLocalization.Get(LocalizationKeys.DiscordHubQueueSummaryTitle),
                AppLocalization.Format(LocalizationKeys.DiscordHubQueueSummaryValue, totalQueued, queues.Count(q => q.Count > 0))));

            foreach (var q in queues.Where(q => q.Count > 0))
            {
                var queueEmoji = q.Count > 5 ? "🔥" : "⏳";
                queueSections.Add((
                    AppLocalization.Format(LocalizationKeys.DiscordHubQueueTitle, queueEmoji, q.Type),
                    AppLocalization.Format(LocalizationKeys.DiscordHubQueueValue, GetNextName(q), q.Count)));
            }
        }

        var component = BuildStatusComponent(
            statusEmoji,
            statusColor,
            summary,
            counts,
            queueSections,
            Context.Client.CurrentUser,
            Context.User);

        await Context.Channel.SendMessageAsync(components: component, flags: MessageFlags.ComponentsV2).ConfigureAwait(false);
    }

    private static MessageComponent BuildStatusComponent(
        string statusEmoji,
        Color statusColor,
        string summary,
        string counts,
        IReadOnlyList<(string Title, string Body)> queueSections,
        IUser? botUser,
        IUser requestingUser)
    {
        var botName = botUser?.Username ?? "SysBot";
        var botAvatar = botUser?.GetAvatarUrl(size: 64) ?? botUser?.GetDefaultAvatarUrl();
        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(statusColor);

        var headerText = TrimComponentText($"**{botName}**\n**{statusEmoji} {AppLocalization.Get(LocalizationKeys.DiscordHubStatusTitle)}**");
        if (string.IsNullOrWhiteSpace(botAvatar))
        {
            container.WithTextDisplay(headerText);
        }
        else
        {
            var header = new SectionBuilder()
                .AddComponent(new TextDisplayBuilder(headerText))
                .WithAccessory(new ThumbnailBuilder(
                    new UnfurledMediaItemProperties(botAvatar),
                    botName,
                    false));

            container.WithSection(header);
        }

        AddStatusSection(container, AppLocalization.Get(LocalizationKeys.DiscordHubSummaryTitle), summary);
        AddStatusSection(container, AppLocalization.Get(LocalizationKeys.DiscordHubCountsTitle), counts);

        foreach (var (title, body) in queueSections)
            AddStatusSection(container, title, body);

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"{requestingUser.Username} • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f>"));

        builder.WithContainer(container);
        return builder.Build();
    }

    private static void AddStatusSection(ContainerBuilder container, string title, string body)
    {
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"{title}\n{body}"));
    }

    private static string TrimComponentText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "\u200B";

        return text.Length <= MaxComponentTextLength ? text : text[..(MaxComponentTextLength - 3)] + "...";
    }

    private static (string Emoji, string Label, Color Color) GetGlobalStatus(bool noBots, bool queuesEmpty)
    {
        if (noBots)
            return ("🟥", AppLocalization.Get(LocalizationKeys.DiscordHubHealthNoBots), Color.DarkRed);
        if (queuesEmpty)
            return ("🟩", AppLocalization.Get(LocalizationKeys.DiscordHubHealthStable), Color.Green);
        return ("🟧", AppLocalization.Get(LocalizationKeys.DiscordHubHealthOperational), Color.Orange);
    }

    private static string GetNextName(PokeTradeQueue<T> q)
    {
        var next = q.TryPeek(out var detail, out _);
        if (!next)
            return AppLocalization.Get(LocalizationKeys.DiscordNone);

        var name = detail.Trainer.TrainerName;

        // show detail of trade if possible
        var nick = detail.TradeData.Nickname;
        if (!string.IsNullOrEmpty(nick))
            name += $" - {nick}";
        return name;
    }

    private static string SummarizeBotsWithBadges(IReadOnlyCollection<RoutineExecutor<PokeBotState>> bots)
    {
        if (bots.Count == 0)
            return string.Empty;

        var lines = bots.Select(z =>
        {
            var summary = z.GetSummary();
            var emoji = GetStatusEmojiFromSummary(summary);
            return $"{emoji} {summary}";
        });

        return string.Join(Environment.NewLine, lines);
    }

    private static string GetStatusEmojiFromSummary(string? summary)
    {
        var text = summary?.ToLowerInvariant() ?? string.Empty;

        if (text.Contains("idle") || text.Contains("inactivo"))
            return "✅";
        if (text.Contains("busy") || text.Contains("running") || text.Contains("trading") || text.Contains("ejecut"))
            return "🔄";
        if (text.Contains("error") || text.Contains("stopped") || text.Contains("unknown") || text.Contains("deten") || text.Contains("desconoc"))
            return "⚠️";
        return "ℹ️";
    }
}
