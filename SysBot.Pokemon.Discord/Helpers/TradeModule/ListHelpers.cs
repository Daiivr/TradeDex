using Discord;
using Discord.Commands;
using Discord.Net;
using PKHeX.Core;
using SysBot.Pokemon.Localization;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class ListHelpers<T> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
    private const string NoticeThumbnailUrl = "https://i.imgur.com/DWLEXyu.png";
    private const string ErrorImageUrl = "https://c.tenor.com/rDzirQgBPwcAAAAd/tenor.gif";

    public static async Task HandleListCommandAsync(SocketCommandContext context, string folderPath, string itemType,
        string commandPrefix, string args)
    {
        const int itemsPerPage = 20;
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;

        if (string.IsNullOrEmpty(folderPath))
        {
            var reply = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordFeatureNotSetup, context.User.Mention), Color.Red, includeErrorImage: true).ConfigureAwait(false);
            _ = Helpers<T>.DeleteMessagesAfterDelayAsync(reply, context.Message, 10);
            return;
        }

        var (filter, page) = Helpers<T>.ParseListArguments(args);

        var allFiles = Directory.GetFiles(folderPath)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(file => file != null)
            .OrderBy(file => file)
            .ToList()!;

        var filteredFiles = allFiles
            .Where(file => file != null && (string.IsNullOrWhiteSpace(filter) ||
                   file.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (filteredFiles.Count == 0)
        {
            var replyMessage = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordNoListMatches, context.User.Mention, itemType, filter), Color.Orange, includeErrorImage: true).ConfigureAwait(false);
            _ = Helpers<T>.DeleteMessagesAfterDelayAsync(replyMessage, context.Message, 10);
            return;
        }

        var pageCount = (int)Math.Ceiling(filteredFiles.Count / (double)itemsPerPage);
        page = Math.Clamp(page, 1, pageCount);

        var pageItems = filteredFiles.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordAvailableListTitle, char.ToUpper(itemType[0]) + itemType[1..], filter))
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordPageOf, page, pageCount))
            .WithColor(Color.Blue);

        foreach (var item in pageItems)
        {
            var index = allFiles.IndexOf(item) + 1;
            embed.AddField($"{index}. {item}", AppLocalization.Format(LocalizationKeys.DiscordUseRequestCommand, botPrefix, commandPrefix, index, itemType.TrimEnd('s')));
        }

        await SendDMOrReplyAsync(context, embed.Build());
    }

    public static async Task SendDMOrReplyAsync(SocketCommandContext context, Embed embed)
    {
        IUserMessage replyMessage;

        if (context.User is IUser user)
        {
            try
            {
                var dmChannel = await user.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: embed);
                replyMessage = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordDmSent, context.User.Mention), Color.Green).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                replyMessage = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordDmBlocked, context.User.Mention), Color.Red, includeErrorImage: true).ConfigureAwait(false);
            }
        }
        else
        {
            replyMessage = await SendNoticeEmbedAsync(context, AppLocalization.Get(LocalizationKeys.DiscordDmError), Color.Red, includeErrorImage: true).ConfigureAwait(false);
        }

        _ = Helpers<T>.DeleteMessagesAfterDelayAsync(replyMessage, context.Message, 10);
    }

    public static async Task HandleRequestCommandAsync(SocketCommandContext context, string folderPath, int index,
        string itemType, string listCommand)
    {
        var userID = context.User.Id;
        if (!await Helpers<T>.EnsureUserNotInQueueAsync(userID))
        {
            await Helpers<T>.SendAlreadyInQueueEmbedAsync(context).ConfigureAwait(false);
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                var reply = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordFeatureNotSetup, context.User.Mention), Color.Red, includeErrorImage: true).ConfigureAwait(false);
                _ = Helpers<T>.DeleteMessagesAfterDelayAsync(reply, context.Message, 10);
                return;
            }

            var files = Directory.GetFiles(folderPath)
                .Select(Path.GetFileName)
                .Where(x => x != null)
                .OrderBy(x => x)
                .ToList()!;

            if (index < 1 || index > files.Count)
            {
                var reply = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordInvalidListIndex, context.User.Mention, itemType, listCommand), Color.Orange, includeErrorImage: true).ConfigureAwait(false);
                _ = Helpers<T>.DeleteMessagesAfterDelayAsync(reply, context.Message, 10);
                return;
            }

            var selectedFile = files[index - 1];
            var fileData = await File.ReadAllBytesAsync(Path.Combine(folderPath, selectedFile!));
            var download = new Download<PKM>
            {
                Data = EntityFormat.GetFromBytes(fileData),
                Success = true
            };

            var pk = Helpers<T>.GetRequest(download);
            if (pk == null)
            {
                var reply = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordListConvertFailed, itemType), Color.Red, includeErrorImage: true).ConfigureAwait(false);
                _ = Helpers<T>.DeleteMessagesAfterDelayAsync(reply, context.Message, 10);
                return;
            }

            var code = Info.GetRandomTradeCode(userID);
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = context.User.GetFavor();

            await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordListRequestAdded, context.User.Mention, char.ToUpper(itemType[0]) + itemType[1..]), Color.Green).ConfigureAwait(false);
            await Helpers<T>.AddTradeToQueueAsync(context, code, context.User.Username, pk, sig,
                context.User, lgcode: lgcode).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var reply = await SendNoticeEmbedAsync(context, AppLocalization.Format(LocalizationKeys.DiscordGenericError, ex.Message), Color.Red, includeErrorImage: true).ConfigureAwait(false);
            _ = Helpers<T>.DeleteMessagesAfterDelayAsync(reply, context.Message, 10);
        }
        finally
        {
            if (context.Message is IUserMessage userMessage)
                _ = Helpers<T>.DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }

    private static async Task<IUserMessage> SendNoticeEmbedAsync(SocketCommandContext context, string description, Color color, bool includeErrorImage = false)
    {
        var embed = new EmbedBuilder()
            .WithColor(color)
            .WithDescription(description)
            .WithThumbnailUrl(NoticeThumbnailUrl)
            .WithFooter(footer =>
            {
                footer.Text = $"{context.User.Username} • {DateTime.UtcNow:hh:mm tt}";
                footer.IconUrl = context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl();
            });

        if (includeErrorImage)
            embed.WithImageUrl(ErrorImageUrl);

        return await context.Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }
}
