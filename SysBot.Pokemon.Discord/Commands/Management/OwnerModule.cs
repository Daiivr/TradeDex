using AnimatedGif;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord;

public class OwnerModule<T> : SudoModule<T> where T : PKM, new()
{
    [Command("listguilds")]
    [Alias("lg", "servers", "listservers")]
    [Summary("Lists all guilds the bot is part of.")]
    [RequireSudo]
    public async Task ListGuilds(int page = 1)
    {
        const int guildsPerPage = 25; // Discord limit for fields in an embed
        int guildCount = Context.Client.Guilds.Count;
        int totalPages = (int)Math.Ceiling(guildCount / (double)guildsPerPage);
        page = Math.Max(1, Math.Min(page, totalPages));

        var guilds = Context.Client.Guilds
            .Skip((page - 1) * guildsPerPage)
            .Take(guildsPerPage);

        var embedBuilder = new EmbedBuilder()
            .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordOwnerGuildListTitle, page, totalPages))
            .WithDescription(AppLocalization.Get(LocalizationKeys.DiscordOwnerGuildListDescription))
            .WithColor((DiscordColor)Color.Blue);

        foreach (var guild in guilds)
        {
            embedBuilder.AddField(guild.Name, AppLocalization.Format(LocalizationKeys.DiscordOwnerGuildId, guild.Id), inline: true);
        }
        var dmChannel = await Context.User.CreateDMChannelAsync();
        await dmChannel.SendMessageAsync(embed: embedBuilder.Build());

        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerGuildListDmSent, Context.User.Mention, page));

        if (Context.Message is IUserMessage userMessage)
        {
            await Task.Delay(2000);
            await userMessage.DeleteAsync().ConfigureAwait(false);
        }
    }

    [Command("blacklistserver")]
    [Alias("bls")]
    [Summary("Adds a server ID to the bot's server blacklist.")]
    [RequireOwner]
    public async Task BlacklistServer(ulong serverId)
    {
        var settings = SysCord<T>.Runner.Hub.Config.Discord;

        if (settings.ServerBlacklist.Contains(serverId))
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerServerAlreadyBlacklisted));
            return;
        }

        var server = Context.Client.GetGuild(serverId);
        if (server == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerServerNotFound));
            return;
        }

        var newServerAccess = new RemoteControlAccess { ID = serverId, Name = server.Name, Comment = AppLocalization.Get(LocalizationKeys.DiscordOwnerBlacklistedServerComment) };

        settings.ServerBlacklist.AddIfNew([newServerAccess]);

        await server.LeaveAsync();
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerServerLeftBlacklisted, server.Name));
    }

    [Command("unblacklistserver")]
    [Alias("ubls")]
    [Summary("Removes a server ID from the bot's server blacklist.")]
    [RequireOwner]
    public async Task UnblacklistServer(ulong serverId)
    {
        var settings = SysCord<T>.Runner.Hub.Config.Discord;

        if (!settings.ServerBlacklist.Contains(serverId))
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerServerNotBlacklisted));
            return;
        }

        var wasRemoved = settings.ServerBlacklist.RemoveAll(x => x.ID == serverId) > 0;

        if (wasRemoved)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerServerRemovedBlacklist, serverId));
        }
        else
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerServerRemoveFailed));
        }
    }

    [Command("addSudo")]
    [Summary("Adds mentioned user to global sudo")]
    [RequireOwner]
    public async Task SudoUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordDone)).ConfigureAwait(false);
    }

    [Command("removeSudo")]
    [Summary("Removes mentioned user from global sudo")]
    [RequireOwner]
    public async Task RemoveSudoUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordDone)).ConfigureAwait(false);
    }

    [Command("addChannel")]
    [Summary("Adds a channel to the list of channels that are accepting commands.")]
    [RequireOwner]
    public async Task AddChannel()
    {
        var obj = GetReference(Context.Message.Channel);
        SysCordSettings.Settings.ChannelWhitelist.AddIfNew([obj]);
        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordDone)).ConfigureAwait(false);
    }

    [Command("syncChannels")]
    [Alias("sch", "syncchannels")]
    [Summary("Copies all channels from ChannelWhitelist to AnnouncementChannel.")]
    [RequireOwner]
    public async Task SyncChannels()
    {
        var whitelist = SysCordSettings.Settings.ChannelWhitelist.List;
        var announcementList = SysCordSettings.Settings.AnnouncementChannels.List;

        bool changesMade = false;

        foreach (var channel in whitelist)
        {
            if (!announcementList.Any(x => x.ID == channel.ID))
            {
                announcementList.Add(channel);
                changesMade = true;
            }
        }

        if (changesMade)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerChannelsSynced)).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerChannelsAlreadySynced)).ConfigureAwait(false);
        }
    }

    [Command("removeChannel")]
    [Summary("Removes a channel from the list of channels that are accepting commands.")]
    [RequireOwner]
    public async Task RemoveChannel()
    {
        var obj = GetReference(Context.Message.Channel);
        SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordDone)).ConfigureAwait(false);
    }

    [Command("leave")]
    [Alias("bye")]
    [Summary("Leaves the current server.")]
    [RequireOwner]
    public async Task Leave()
    {
        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerGoodbye)).ConfigureAwait(false);
        await Context.Guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("leaveguild")]
    [Alias("lg")]
    [Summary("Leaves guild based on supplied ID.")]
    [RequireOwner]
    public async Task LeaveGuild(string userInput)
    {
        if (!ulong.TryParse(userInput, out ulong id))
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerValidGuildIdRequired)).ConfigureAwait(false);
            return;
        }

        var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerInvalidGuild, userInput)).ConfigureAwait(false);
            return;
        }

        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerLeavingGuild, guild)).ConfigureAwait(false);
        await guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("leaveall")]
    [Summary("Leaves all servers the bot is currently in.")]
    [RequireOwner]
    public async Task LeaveAll()
    {
        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerLeavingAllGuilds)).ConfigureAwait(false);
        foreach (var guild in Context.Client.Guilds)
        {
            await guild.LeaveAsync().ConfigureAwait(false);
        }
    }

    [Command("repeek")]
    [Alias("peek")]
    [Summary("Take and send a screenshot from the currently configured Switch.")]
    [RequireSudo]
    public async Task RePeek(string? address = null)
    {
        string ip = string.IsNullOrWhiteSpace(address) ? OwnerModule<T>.GetBotIPFromJsonConfig() : address.Trim();
        var source = new CancellationTokenSource();
        var token = source.Token;

        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerNoBotIp, ip)).ConfigureAwait(false);
            return;
        }

        _ = Array.Empty<byte>();
        byte[]? bytes;
        try
        {
            bytes = await bot.Bot.Connection.PixelPeek(token).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerPixelFetchError, ex.Message));
            return;
        }

        if (bytes.Length == 0)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerNoScreenshotData));
            return;
        }

        await using MemoryStream ms = new(bytes);
        const string img = "cap.jpg";
        var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = (DiscordColor?)Color.Purple }
            .WithFooter(new EmbedFooterBuilder { Text = AppLocalization.Get(LocalizationKeys.DiscordOwnerScreenshotFooter) });

        await Context.Channel.SendFileAsync(ms, img, embed: embed.Build());
    }

    [Command("video")]
    [Alias("video")]
    [Summary("Take and send a GIF from the currently configured Switch.")]
    [RequireSudo]
    public async Task RePeekGIF()
    {
        await Context.Channel.SendMessageAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerGifProcessing)).ConfigureAwait(false);

        try
        {
            string ip = OwnerModule<T>.GetBotIPFromJsonConfig();
            var source = new CancellationTokenSource();
            var token = source.Token;
            var bot = SysCord<T>.Runner.GetBot(ip);

            if (bot == null)
            {
                await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerNoBotIp, ip)).ConfigureAwait(false);
                return;
            }

            const int screenshotCount = 10;
            var screenshotInterval = TimeSpan.FromSeconds(0.1 / 10);
            var gifFrames = new List<byte[]>();

            for (int i = 0; i < screenshotCount; i++)
            {
                byte[] bytes;
                try
                {
                    bytes = await bot.Bot.Connection.PixelPeek(token).ConfigureAwait(false) ?? Array.Empty<byte>();
                }
                catch (Exception ex)
                {
                    await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerPixelFetchError, ex.Message)).ConfigureAwait(false);
                    return;
                }

                if (bytes.Length == 0)
                {
                    await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerNoScreenshotData)).ConfigureAwait(false);
                    return;
                }

                gifFrames.Add(bytes);

                if (i < screenshotCount - 1)
                {
                    await Task.Delay(screenshotInterval).ConfigureAwait(false);
                }
            }

            await using (var ms = new MemoryStream())
            {
                await CreateGifAsync(ms, gifFrames).ConfigureAwait(false);

                ms.Position = 0;
                const string gifFileName = "screenshot.gif";
                var embed = new EmbedBuilder { ImageUrl = $"attachment://{gifFileName}", Color = (DiscordColor?)Color.Red }
                    .WithFooter(new EmbedFooterBuilder { Text = AppLocalization.Get(LocalizationKeys.DiscordOwnerGifFooter) });

                await Context.Channel.SendFileAsync(ms, gifFileName, embed: embed.Build()).ConfigureAwait(false);
            }

            gifFrames.Clear();
        }
        catch (Exception ex)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerGifProcessingError, ex.Message)).ConfigureAwait(false);
        }
    }

    private async Task CreateGifAsync(Stream outputStream, List<byte[]> frames)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        using var gif = new AnimatedGifCreator(outputStream, 200);
        foreach (var frameBytes in frames)
        {
            using (var ms = new MemoryStream(frameBytes))
            using (var bitmap = new Bitmap(ms))
            using (var frame = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                gif.AddFrame(frame);
            }
            await Task.Yield(); // Allow other tasks to run
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }

    private static string GetBotIPFromJsonConfig()
    {
        try
        {
            var jsonData = File.ReadAllText(TradeBot.ConfigPath);
            var config = JObject.Parse(jsonData);

            var botsArray = config["Bots"] as JArray;
            if (botsArray == null || botsArray.Count == 0)
                return "192.168.1.1";

            var firstBot = botsArray[0] as JObject;
            var connection = firstBot?["Connection"] as JObject;
            var ip = connection?["IP"]?.ToString();

            return ip ?? "192.168.1.1";
        }
        catch (Exception ex)
        {
            Console.WriteLine(AppLocalization.Format(LocalizationKeys.LogBotConfigReadError, ex.Message));
            return "192.168.1.1";
        }
    }

    [Command("kill")]
    [Alias("shutdown")]
    [Summary("Causes the entire process to end itself!")]
    [RequireOwner]
    public async Task ExitProgram()
    {
        await Context.Channel.EchoAndReply(AppLocalization.Get(LocalizationKeys.DiscordOwnerShutdown)).ConfigureAwait(false);
        Environment.Exit(0);
    }

    [Command("dm")]
    [Summary("Sends a direct message to a specified user.")]
    [RequireOwner]
    public async Task DMUserAsync(SocketUser user, [Remainder] string message)
    {
        var attachments = Context.Message.Attachments;
        List<string> imageUrls = [];
        List<string> nonImageAttachmentUrls = [];

        foreach (var attachment in attachments)
        {
            if (IsImageAttachment(attachment.Filename))
            {
                if (imageUrls.Count < 3)
                    imageUrls.Add(attachment.Url);
            }
            else
            {
                nonImageAttachmentUrls.Add(attachment.Url);
            }
        }

        var embed = new EmbedBuilder
        {
            Title = AppLocalization.Get(LocalizationKeys.DiscordOwnerDmTitle),
            Description = AppLocalization.Format(LocalizationKeys.DiscordOwnerDmDescription, message),
            Color = (DiscordColor?)Color.Gold,
            Timestamp = DateTimeOffset.Now,
            ThumbnailUrl = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/pikamail.png"
        };

        if (imageUrls.Count > 0)
            embed.ImageUrl = imageUrls[0];

        for (int i = 1; i < imageUrls.Count; i++)
        {
            embed.AddField(
                AppLocalization.Format(LocalizationKeys.DiscordOwnerDmAdditionalImage, i),
                AppLocalization.Format(LocalizationKeys.DiscordOwnerDmViewImage, imageUrls[i]));
        }

        foreach (var url in nonImageAttachmentUrls)
            embed.AddField(AppLocalization.Get(LocalizationKeys.DiscordOwnerDmDownloadLink), url);

        try
        {
            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed.Build());

            var confirmationMessage = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerDmSent, user.Username));
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromSeconds(10));
            await confirmationMessage.DeleteAsync();
        }
        catch (Exception ex)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerDmFailed, user.Username, ex.Message));
        }
    }

    private static bool IsImageAttachment(string filename)
    {
        var extension = Path.GetExtension(filename);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    [Command("say")]
    [Summary("Sends a message to a specified channel.")]
    [RequireSudo]
    public async Task SayAsync([Remainder] string message)
    {
        var attachments = Context.Message.Attachments;
        var hasAttachments = attachments.Count != 0;

        var indexOfChannelMentionStart = message.LastIndexOf('<');
        var indexOfChannelMentionEnd = message.LastIndexOf('>');
        if (indexOfChannelMentionStart == -1 || indexOfChannelMentionEnd == -1)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerMentionChannel, Context.User.Mention));
            return;
        }

        var channelMention = message.Substring(indexOfChannelMentionStart, indexOfChannelMentionEnd - indexOfChannelMentionStart + 1);
        var actualMessage = message.Substring(0, indexOfChannelMentionStart).TrimEnd();

        var channel = Context.Guild.Channels.FirstOrDefault(c => $"<#{c.Id}>" == channelMention);

        if (channel == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerChannelNotFound));
            return;
        }

        if (channel is not IMessageChannel messageChannel)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerChannelNotText));
            return;
        }

        // If there are attachments, send them to the channel
        if (hasAttachments)
        {
            foreach (var attachment in attachments)
            {
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(attachment.Url);
                var file = new FileAttachment(stream, attachment.Filename);
                await messageChannel.SendFileAsync(file, actualMessage);
            }
        }
        else
        {
            await messageChannel.SendMessageAsync(actualMessage);
        }

        // Send confirmation message to the user
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerMessagePosted, Context.User.Mention, channelMention));
    }

    private RemoteControlAccess GetReference(IUser channel) => new()
    {
        ID = channel.Id,
        Name = channel.Username,
        Comment = AppLocalization.Format(LocalizationKeys.DiscordReferenceAddedBy, Context.User.Username, DateTime.Now),
    };

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = AppLocalization.Format(LocalizationKeys.DiscordReferenceAddedBy, Context.User.Username, DateTime.Now),
    };

    [Command("startController")]
    [Alias("controllerstart", "startcontrol", "controlstart", "startremote", "remotestart", "sbr", "controller")]
    [Summary("Makes the bot open Switch Remote for PC - a GUI game controller for your Switch.")]
    [RequireOwner]
    public async Task StartSysRemote()
    {
        try
        {
            var folders = SysCord<T>.Runner.Config.Folder;
            var sysBotRemotePath = folders.SwitchRemoteForPC;

            if (string.IsNullOrWhiteSpace(sysBotRemotePath))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerRemotePathMissing));
                return;
            }

            if (!Directory.Exists(sysBotRemotePath))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerRemotePathInvalid));
                return;
            }

            string executablePath = Path.Combine(sysBotRemotePath, "SwitchRemoteForPC.exe");

            if (!File.Exists(executablePath))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerRemoteExeMissing));
                return;
            }

            var startInfo = new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = sysBotRemotePath
            };
            Process.Start(startInfo);

            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordOwnerRemoteStarted));
        }
        catch (Exception ex)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordOwnerRemoteError, ex.Message));
        }
    }

}
