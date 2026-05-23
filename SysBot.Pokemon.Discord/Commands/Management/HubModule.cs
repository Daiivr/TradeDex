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
        var (statusEmoji, statusLabel, embedColor) = GetGlobalStatus(noBots, queuesEmpty);

        var builder = new EmbedBuilder()
            .WithTitle($"{statusEmoji} {AppLocalization.Get(LocalizationKeys.DiscordHubStatusTitle)}")
            .WithColor(embedColor)
            .WithCurrentTimestamp();

        var currentUser = Context.Client.CurrentUser;
        builder.WithAuthor(author =>
        {
            author.Name = currentUser?.Username ?? "SysBot";
            author.IconUrl = currentUser?.GetAvatarUrl() ?? currentUser?.GetDefaultAvatarUrl();
        });

        var botsSummary = SummarizeBotsWithBadges(allBots);
        var botsSection = string.IsNullOrWhiteSpace(botsSummary)
            ? string.Empty
            : $"\n```{Environment.NewLine}{botsSummary}{Environment.NewLine}```";

        builder.AddField(x =>
        {
            x.Name = AppLocalization.Get(LocalizationKeys.DiscordHubSummaryTitle);
            x.Value = AppLocalization.Format(LocalizationKeys.DiscordHubSummaryValue, botCount, botsSection, statusLabel, hub.Ledy.Pool.Count);
            x.IsInline = false;
        });

        builder.AddField(x =>
        {
            var bots = allBots.OfType<ICountBot>();
            var lines = bots.SelectMany(z => z.Counts.GetNonZeroCounts()).Distinct();
            var msg = string.Join("\n", lines);
            if (string.IsNullOrWhiteSpace(msg))
                msg = AppLocalization.Get(LocalizationKeys.DiscordHubNothingCounted);
            x.Name = AppLocalization.Get(LocalizationKeys.DiscordHubCountsTitle);
            x.Value = msg;
            x.IsInline = false;
        });

        var totalQueued = queues.Sum(q => q.Count);
        if (totalQueued == 0)
        {
            builder.AddField(x =>
            {
                x.Name = AppLocalization.Get(LocalizationKeys.DiscordHubQueuesEmptyTitle);
                x.Value = AppLocalization.Get(LocalizationKeys.DiscordHubQueuesEmptyValue);
                x.IsInline = false;
            });
        }
        else
        {
            builder.AddField(x =>
            {
                x.Name = AppLocalization.Get(LocalizationKeys.DiscordHubQueueSummaryTitle);
                x.Value = AppLocalization.Format(LocalizationKeys.DiscordHubQueueSummaryValue, totalQueued, queues.Count(q => q.Count > 0));
                x.IsInline = false;
            });

            foreach (var q in queues.Where(q => q.Count > 0))
            {
                var queueEmoji = q.Count > 5 ? "🔥" : "⏳";

                builder.AddField(x =>
                {
                    x.Name = AppLocalization.Format(LocalizationKeys.DiscordHubQueueTitle, queueEmoji, q.Type);
                    x.Value = AppLocalization.Format(LocalizationKeys.DiscordHubQueueValue, GetNextName(q), q.Count);
                    x.IsInline = false;
                });
            }
        }

        await ReplyAsync(embed: builder.Build()).ConfigureAwait(false);
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
