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
        var me = SysCord<T>.Runner;
        var hub = me.Hub;

        var builder = new EmbedBuilder
        {
            Color = Color.Gold,
        };

        var runner = SysCord<T>.Runner;
        var allBots = runner.Bots.ConvertAll(z => z.Bot);
        var botCount = allBots.Count;
        builder.AddField(x =>
        {
            x.Name = AppLocalization.Get(LocalizationKeys.DiscordHubSummaryTitle);
            x.Value = AppLocalization.Format(LocalizationKeys.DiscordHubSummaryValue, botCount, SummarizeBots(allBots), hub.Ledy.Pool.Count);
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

        var queues = hub.Queues.AllQueues;
        int count = 0;
        foreach (var q in queues)
        {
            var c = q.Count;
            if (c == 0)
                continue;

            var nextMsg = GetNextName(q);
            builder.AddField(x =>
            {
                x.Name = AppLocalization.Format(LocalizationKeys.DiscordHubQueueTitle, q.Type);
                x.Value = AppLocalization.Format(LocalizationKeys.DiscordHubQueueValue, nextMsg, c);
                x.IsInline = false;
            });
            count += c;
        }

        if (count == 0)
        {
            builder.AddField(x =>
            {
                x.Name = AppLocalization.Get(LocalizationKeys.DiscordHubQueuesEmptyTitle);
                x.Value = AppLocalization.Get(LocalizationKeys.DiscordHubQueuesEmptyValue);
                x.IsInline = false;
            });
        }

        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordHubStatusTitle), false, builder.Build()).ConfigureAwait(false);
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

    private static string SummarizeBots(IReadOnlyCollection<RoutineExecutor<PokeBotState>> bots)
    {
        if (bots.Count == 0)
            return AppLocalization.Get(LocalizationKeys.DiscordBotNoConfigured);
        var summaries = bots.Select(z => $"- {z.GetSummary()}");
        return Environment.NewLine + string.Join(Environment.NewLine, summaries);
    }
}
