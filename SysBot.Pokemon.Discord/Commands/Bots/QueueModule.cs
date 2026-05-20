using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Clears and toggles Queue features.")]
public class QueueModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("queueMode")]
    [Alias("qm")]
    [Summary("Changes how queueing is controlled (manual/threshold/interval).")]
    [RequireSudo]
    public async Task ChangeQueueModeAsync([Summary("Queue mode")] QueueOpening mode)
    {
        SysCord<T>.Runner.Hub.Config.Queues.QueueToggleMode = mode;
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordQueueModeChanged, mode)).ConfigureAwait(false);
    }

    [Command("queueClearAll")]
    [Alias("qca", "tca")]
    [Summary("Clears all users from the trade queues.")]
    [RequireSudo]
    public async Task ClearAllTradesAsync()
    {
        Info.ClearAllQueues();
        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordQueueClearedAll)).ConfigureAwait(false);
    }

    [Command("queueClear")]
    [Alias("qc", "tc")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    public async Task ClearTradeAsync()
    {
        string msg = ClearTrade(Context.User.Id, Context.User.Mention);
        await ReplyAndDeleteAsync(msg, 5, Context.Message).ConfigureAwait(false);
    }

    [Command("queueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync([Summary("Discord user ID")] ulong id)
    {
        string msg = ClearTrade(id, MentionUtils.MentionUser(id));
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    [Command("queueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync([Summary("Username of the person to clear")] string _)
    {
        foreach (var user in Context.Message.MentionedUsers)
        {
            string msg = ClearTrade(user.Id, user.Mention);
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }

    [Command("queueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync()
    {
        var users = Context.Message.MentionedUsers;
        if (users.Count == 0)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordNoUsersMentioned)).ConfigureAwait(false);
            return;
        }
        foreach (var u in users)
            await ClearTradeUserAsync(u.Id).ConfigureAwait(false);
    }

    [Command("deleteTradeCode")]
    [Alias("dtc")]
    [Summary("Deletes the stored trade code for the user.")]
    public async Task DeleteTradeCodeAsync()
    {
        await DeleteTradeCode(Context.User.Id, Context.User).ConfigureAwait(false);

        if (Context.Message is IUserMessage userMessage)
            await userMessage.DeleteAsync().ConfigureAwait(false);
    }

    [Command("queueStatus")]
    [Alias("qs", "ts")]
    [Summary("Checks the user's position in the queue.")]
    public async Task GetTradePositionAsync()
    {
        var userID = Context.User.Id;
        var tradeEntry = Info.GetDetail(userID);

        string msg;
        if (tradeEntry != null)
        {
            var uniqueTradeID = tradeEntry.UniqueTradeID;
            msg = Context.User.Mention + " - " + Info.GetPositionString(userID, uniqueTradeID, tradeEntry.Type);
        }
        else
        {
            msg = Context.User.Mention + " - " + AppLocalization.Get(LocalizationKeys.DiscordNotCurrentlyInQueue);
        }

        await ReplyAndDeleteAsync(msg, 5, Context.Message).ConfigureAwait(false);
    }

    [Command("queueList")]
    [Alias("ql")]
    [Summary("Shows a nice embed of the current queue with species, trade type, and username.")]
    [RequireSudo]
    public async Task ListUserQueue()
    {
        var lines = SysCord<T>.Runner.Hub.Queues.Info
            .GetUserList("(ID {0}) - Code: {1} - {2} - {3}")
            .ToList();

        var total = lines.Count;
        if (total == 0)
        {
            var emptyEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordQueueListEmptyTitle))
                .WithDescription(AppLocalization.Get(LocalizationKeys.DiscordQueueListEmptyDescription))
                .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordQueueListTotalFooter, 0))
                .WithThumbnailUrl("https://i.imgur.com/haOeRR9.gif")
                .WithCurrentTimestamp()
                .Build();

            try
            {
                await Context.User.SendMessageAsync(embed: emptyEmbed).ConfigureAwait(false);
                await Context.Message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);
                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordQueueListEmptyDmSent, Context.User.Mention)).ConfigureAwait(false);
            }
            catch
            {
                await Context.Message.AddReactionAsync(new Emoji("❌")).ConfigureAwait(false);
                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordQueueListDmFailed, Context.User.Mention)).ConfigureAwait(false);
            }
            return;
        }

        const int maxEmbedDescription = 4096;
        const int maxLinesPerPage = 25;
        var pages = BuildQueuePages(lines, maxLinesPerPage, maxEmbedDescription - 200);

        try
        {
            var pageIndex = 1;
            foreach (var page in pages)
            {
                var embed = new EmbedBuilder()
                    .WithColor(Color.Blue)
                    .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordQueueListPageTitle, pageIndex, pages.Count))
                    .WithDescription($"```{page}```")
                    .WithThumbnailUrl("https://i.imgur.com/Zs9hmNq.gif")
                    .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordQueueListTotalFooter, total))
                    .WithCurrentTimestamp()
                    .Build();

                await Context.User.SendMessageAsync(embed: embed).ConfigureAwait(false);
                pageIndex++;
            }

            await Context.Message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordQueueListDmSentTotal, Context.User.Mention, total)).ConfigureAwait(false);
        }
        catch
        {
            await Context.Message.AddReactionAsync(new Emoji("❌")).ConfigureAwait(false);
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordQueueListDmFailed, Context.User.Mention)).ConfigureAwait(false);
        }
    }

    [Command("queueToggle")]
    [Alias("qt")]
    [Summary("Toggles on/off the ability to join the trade queue.")]
    [RequireSudo]
    public Task ToggleQueueTradeAsync()
    {
        var state = Info.ToggleQueue();
        var msg = state
            ? AppLocalization.Format(LocalizationKeys.DiscordQueueEnabled, Context.User.Mention)
            : AppLocalization.Format(LocalizationKeys.DiscordQueueDisabled, Context.User.Mention);

        return Context.Channel.EchoAndReply(msg);
    }

    [Command("addTradeCode")]
    [Alias("atc")]
    [Summary("Stores a trade code for the user.")]
    public async Task AddTradeCodeAsync([Summary("Trade code to store.")] string tradeCode)
    {
        if (!TryParseTradeCode(tradeCode, out var parsedCode))
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeRangeMention, Context.User.Mention)).ConfigureAwait(false);
        }
        else
        {
            await AddTradeCode(Context.User.Id, parsedCode, Context.User, FormatTradeCode(parsedCode)).ConfigureAwait(false);
        }

        if (Context.Message is IUserMessage userMessage)
            await userMessage.DeleteAsync().ConfigureAwait(false);
    }

    [Command("updateTradeCode")]
    [Alias("utc", "changeTradeCode", "ctc")]
    [Summary("Updates the stored trade code for the user.")]
    public async Task UpdateTradeCodeAsync([Summary("New trade code to store.")] string newTradeCode)
    {
        if (!TryParseTradeCode(newTradeCode, out var parsedCode))
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeRangeMention, Context.User.Mention)).ConfigureAwait(false);
        }
        else
        {
            await UpdateTradeCode(Context.User.Id, parsedCode, Context.User, FormatTradeCode(parsedCode)).ConfigureAwait(false);
        }

        if (Context.Message is IUserMessage userMessage)
            await userMessage.DeleteAsync().ConfigureAwait(false);
    }

    private static string ClearTrade(ulong userID, string userMention)
    {
        var result = Info.ClearTrade(userID);
        return GetClearTradeMessage(result, userMention);
    }

    private static List<string> BuildQueuePages(List<string> lines, int maxLinesPerPage, int maxCharsPerPage)
    {
        var pages = new List<string>();
        var builder = new StringBuilder();
        var lineCountInPage = 0;

        foreach (var line in lines)
        {
            var needNewPage = lineCountInPage >= maxLinesPerPage || builder.Length + line.Length + 1 > maxCharsPerPage;
            if (needNewPage)
            {
                if (builder.Length > 0)
                    pages.Add(builder.ToString());

                builder.Clear();
                lineCountInPage = 0;
            }

            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append(line);
            lineCountInPage++;
        }

        if (builder.Length > 0)
            pages.Add(builder.ToString());

        return pages;
    }

    private static async Task AddTradeCode(ulong userID, int tradeCode, IUser user, string formattedCode)
    {
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;
        var tradeCodeStorage = new TradeCodeStorage();
        bool success = tradeCodeStorage.SetTradeCode(userID, tradeCode);

        var embedBuilder = new EmbedBuilder();
        if (success)
        {
            embedBuilder.WithColor(Color.Green)
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeStoredTitle))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeStoredDescription, user.Mention, formattedCode))
                .WithThumbnailUrl("https://i.imgur.com/Zs9hmNq.gif");
        }
        else
        {
            int existingTradeCode = tradeCodeStorage.GetTradeCode(userID);
            string formattedExistingCode = FormatTradeCode(existingTradeCode);
            embedBuilder.WithColor(Color.Red)
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeExistingTitle))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeExistingDescription, user.Mention))
                .AddField(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeExistingField), AppLocalization.Format(LocalizationKeys.DiscordTradeCodeExistingValue, formattedExistingCode), true)
                .AddField("\u200B", "\u200B", true)
                .AddField(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeSolutionField), AppLocalization.Format(LocalizationKeys.DiscordTradeCodeAddSolution, botPrefix), true)
                .WithThumbnailUrl("https://i.imgur.com/haOeRR9.gif");
        }

        await user.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
    }

    private static async Task UpdateTradeCode(ulong userID, int newTradeCode, IUser user, string formattedCode)
    {
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;
        var tradeCodeStorage = new TradeCodeStorage();
        bool success = tradeCodeStorage.UpdateTradeCode(userID, newTradeCode);

        var embedBuilder = new EmbedBuilder();
        if (success)
        {
            embedBuilder.WithColor(Color.Green)
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeUpdateTitle))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeUpdateDescription, user.Mention, formattedCode))
                .WithThumbnailUrl("https://i.imgur.com/Zs9hmNq.gif");
        }
        else
        {
            embedBuilder.WithColor(Color.Red)
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeUpdateErrorTitle))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeUpdateErrorDescription, user.Mention))
                .AddField(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeReasonField), AppLocalization.Get(LocalizationKeys.DiscordTradeCodeMissingReason), true)
                .AddField("\u200B", "\u200B", true)
                .AddField(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeSolutionField), AppLocalization.Format(LocalizationKeys.DiscordTradeCodeUpdateSolution, botPrefix), true)
                .WithThumbnailUrl("https://i.imgur.com/haOeRR9.gif");
        }

        await user.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
    }

    private static async Task DeleteTradeCode(ulong userID, IUser user)
    {
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;
        var tradeCodeStorage = new TradeCodeStorage();
        bool success = tradeCodeStorage.DeleteTradeCode(userID);

        var embedBuilder = new EmbedBuilder();
        if (success)
        {
            embedBuilder.WithColor(Color.Green)
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeDeleteTitle))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeDeleteDescription, user.Mention))
                .WithThumbnailUrl("https://i.imgur.com/Zs9hmNq.gif");
        }
        else
        {
            embedBuilder.WithColor(Color.Red)
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeDeleteErrorTitle))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordTradeCodeDeleteErrorDescription, user.Mention))
                .AddField(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeReasonField), AppLocalization.Get(LocalizationKeys.DiscordTradeCodeDeleteMissingReason), true)
                .AddField("\u200B", "\u200B", true)
                .AddField(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeSolutionField), AppLocalization.Format(LocalizationKeys.DiscordTradeCodeDeleteSolution, botPrefix), true)
                .WithThumbnailUrl("https://i.imgur.com/haOeRR9.gif");
        }

        await user.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
    }

    public static string FormatTradeCode(int code)
    {
        string codeStr = code.ToString("D8");
        return codeStr[..4] + " " + codeStr[4..];
    }

    private static bool TryParseTradeCode(string code, out int tradeCode)
    {
        tradeCode = 0;
        return code.Length is > 0 and <= 8 && code.All(char.IsDigit) && int.TryParse(code, out tradeCode);
    }

    private static string GetClearTradeMessage(QueueResultRemove result, string userMention)
    {
        return result switch
        {
            QueueResultRemove.Removed => AppLocalization.Format(LocalizationKeys.DiscordClearTradeRemoved, userMention),
            QueueResultRemove.CurrentlyProcessing => AppLocalization.Format(LocalizationKeys.DiscordClearTradeProcessing, userMention),
            QueueResultRemove.CurrentlyProcessingRemoved => AppLocalization.Format(LocalizationKeys.DiscordClearTradeProcessingRemoved, userMention),
            QueueResultRemove.NotInQueue => AppLocalization.Format(LocalizationKeys.DiscordClearTradeNotInQueue, userMention),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };
    }

    private async Task DeleteMessagesAfterDelayAsync(IMessage sentMessage, IMessage? messageToDelete, int delaySeconds)
    {
        try
        {
            // Don't attempt to delete messages in DM channels - Discord doesn't allow it
            if (sentMessage.Channel is IDMChannel)
                return;

            await Task.Delay(delaySeconds * 1000);
            await sentMessage.DeleteAsync();
            if (messageToDelete != null)
                await messageToDelete.DeleteAsync();
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(QueueModule<T>));
        }
    }

    private async Task ReplyAndDeleteAsync(string message, int delaySeconds, IMessage? messageToDelete = null)
    {
        try
        {
            var sentMessage = await ReplyAsync(message).ConfigureAwait(false);
            _ = DeleteMessagesAfterDelayAsync(sentMessage, messageToDelete, delaySeconds);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(QueueModule<T>));
        }
    }

}
