using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Pokemon.Discord.Models;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Drawing = System.Drawing;

namespace SysBot.Pokemon.Discord;

public class ProfileModule : ModuleBase<SocketCommandContext>
{
    private const string StatsFilePath = "user_stats.json";
    private static readonly HttpClient Http = new();

    [Command("profile")]
    [Alias("tp")]
    [Summary("Muestra la informacion del perfil de un usuario, con detalles sensibles visibles solo para el propietario del perfil.")]
    public async Task ProfileAsync(IUser? user = null)
    {
        var targetUser = user ?? Context.User;
        var isSelfProfile = targetUser.Id == Context.User.Id;
        var embed = await BuildProfileEmbedAsync(targetUser, isSelfProfile).ConfigureAwait(false);
        var components = BuildProfileComponents("view_badges");

        if (isSelfProfile)
        {
            try
            {
                var dmChannel = await targetUser.CreateDMChannelAsync().ConfigureAwait(false);
                var message = await dmChannel.SendMessageAsync(embed: embed, components: components).ConfigureAwait(false);
                var confirmation = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordProfileSentDm, targetUser.Mention)).ConfigureAwait(false);
                _ = DeleteAfterDelayAsync(confirmation, TimeSpan.FromSeconds(10));
                _ = DeleteAfterDelayAsync(Context.Message, TimeSpan.Zero);
                _ = HandleSelectMenuInteractionsAsync(message, Context.User.Id, TimeSpan.FromMinutes(1), targetUser.Id);
            }
            catch
            {
                var error = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordProfileDmFailed, targetUser.Mention)).ConfigureAwait(false);
                _ = DeleteAfterDelayAsync(error, TimeSpan.FromSeconds(10));
                _ = DeleteAfterDelayAsync(Context.Message, TimeSpan.Zero);
            }

            return;
        }

        var publicMessage = await ReplyAsync(embed: embed, components: components).ConfigureAwait(false);
        _ = HandleSelectMenuInteractionsAsync(publicMessage, Context.User.Id, TimeSpan.FromMinutes(1), targetUser.Id);
    }

    private async Task<Embed> BuildProfileEmbedAsync(IUser targetUser, bool includePrivateInfo)
    {
        var avatarUrl = targetUser.GetAvatarUrl(size: 128) ?? targetUser.GetDefaultAvatarUrl();
        var tradeCount = GetTradeCountForUser(targetUser.Id);
        var badges = GetBadgesForTradeCount(tradeCount);
        var (xp, level) = GetGameStatsForUser(targetUser.Id.ToString());
        var currentStatus = GetCurrentStatus(tradeCount);
        var requiredXp = GetRequiredXPForNextLevel(level);
        var xpProgress = requiredXp <= 0 ? 0 : Math.Clamp((double)xp / requiredXp * 100, 0, 100);
        var accountCreated = $"<t:{targetUser.CreatedAt.ToUnixTimeSeconds()}:R>";
        var tradeDetails = new TradeCodeStorage().GetTradeDetails(targetUser.Id);

        var embed = new EmbedBuilder()
            .WithAuthor(targetUser)
            .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordProfileTitle, targetUser.Username))
            .WithDescription(AppLocalization.Get(LocalizationKeys.DiscordProfileDescription))
            .WithThumbnailUrl(avatarUrl)
            .WithColor(await GetDominantColorAsync(avatarUrl).ConfigureAwait(false))
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileTrades), tradeCount.ToString("N0"), true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileLevel), $"{level} (XP: {xp}/{requiredXp})", true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileCreated), accountCreated, true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileCurrentTitle), currentStatus, true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileLastTrade), GetLastTradeText(tradeDetails), true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileBadges), badges, false)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileProgress), GetProgressBar(xpProgress))
            .WithCurrentTimestamp();

        if (includePrivateInfo)
        {
            var (ot, sid, tid) = GetTrainerInfo(targetUser.Id);
            embed.AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileTrainerInfo), $"**OT**: {ot}\n**SID**: {sid}\n**TID**: {tid}", true)
                 .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileTradeCode), GetTradeCodeForUser(targetUser.Id) ?? AppLocalization.Get(LocalizationKeys.DiscordProfileNoTradeCode), true);
        }

        embed.WithFooter(footer =>
        {
            var serverIconUrl = Context.Guild?.IconUrl;
            var serverName = Context.Guild?.Name ?? AppLocalization.Get(LocalizationKeys.DiscordProfileThisServer);
            footer.WithIconUrl(serverIconUrl);
            footer.WithText(includePrivateInfo
                ? AppLocalization.Format(LocalizationKeys.DiscordProfileServerFooter, serverName)
                : AppLocalization.Format(LocalizationKeys.DiscordProfilePublicFooter, serverName));
        });

        return embed.Build();
    }

    private async Task<Color> GetDominantColorAsync(string imageUrl)
    {
        try
        {
            await using var stream = await Http.GetStreamAsync(imageUrl).ConfigureAwait(false);
            using var bitmap = new Drawing.Bitmap(stream);
            var histogram = new Dictionary<int, int>();

            var stepX = Math.Max(1, bitmap.Width / 32);
            var stepY = Math.Max(1, bitmap.Height / 32);
            for (var y = 0; y < bitmap.Height; y += stepY)
            {
                for (var x = 0; x < bitmap.Width; x += stepX)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.A < 128)
                        continue;

                    var key = (pixel.R << 16) | (pixel.G << 8) | pixel.B;
                    histogram[key] = histogram.GetValueOrDefault(key) + 1;
                }
            }

            if (histogram.Count == 0)
                return Color.Blue;

            var dominant = histogram.OrderByDescending(kvp => kvp.Value).First().Key;
            return new Color((byte)(dominant >> 16), (byte)(dominant >> 8), (byte)dominant);
        }
        catch
        {
            return Color.Blue;
        }
    }

    private static MessageComponent BuildProfileComponents(string option)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId("profile_badges")
            .WithPlaceholder(AppLocalization.Get(LocalizationKeys.DiscordProfileSelectPlaceholder));

        if (option == "back_to_profile")
            menu.AddOption(AppLocalization.Get(LocalizationKeys.DiscordProfileBackToProfile), "back_to_profile", AppLocalization.Get(LocalizationKeys.DiscordProfileBackToProfileDescription), new Emoji("🪪"));
        else
            menu.AddOption(AppLocalization.Get(LocalizationKeys.DiscordProfileViewBadges), "view_badges", AppLocalization.Get(LocalizationKeys.DiscordProfileViewBadgesDescription), new Emoji("🎖️"));

        return new ComponentBuilder().WithSelectMenu(menu).Build();
    }

    private async Task HandleSelectMenuInteractionsAsync(IUserMessage message, ulong userId, TimeSpan timeout, ulong targetUserId)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);

        while (!timeoutCts.IsCancellationRequested)
        {
            var interaction = await WaitForSelectMenuResponseAsync(message, userId, timeoutCts.Token).ConfigureAwait(false);
            if (interaction == null)
                break;

            timeoutCts.CancelAfter(timeout);
            await interaction.DeferAsync().ConfigureAwait(false);
            var selectedOption = interaction.Data.Values.FirstOrDefault();

            if (selectedOption == "view_badges")
                await ShowBadgesAsync(message, targetUserId).ConfigureAwait(false);
            else if (selectedOption == "back_to_profile")
            {
                var targetUser = GetUser(targetUserId) ?? Context.User;
                var embed = await BuildProfileEmbedAsync(targetUser, targetUser.Id == Context.User.Id).ConfigureAwait(false);
                await message.ModifyAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = BuildProfileComponents("view_badges");
                }).ConfigureAwait(false);
            }
        }

        await message.ModifyAsync(msg => msg.Components = new ComponentBuilder().Build()).ConfigureAwait(false);
    }

    private async Task ShowBadgesAsync(IUserMessage message, ulong targetUserId)
    {
        var targetUser = GetUser(targetUserId) ?? Context.User;
        var tradeCount = GetTradeCountForUser(targetUserId);
        var badgeList = SysCordSettings.Settings.CustomBadgeEmojis.OrderBy(b => b.TradeCount).ToList();
        var nextBadge = badgeList.FirstOrDefault(b => b.TradeCount > tradeCount);
        var nextBadgeInfo = nextBadge != null
            ? AppLocalization.Format(LocalizationKeys.DiscordProfileNextBadgeInfo, nextBadge.TradeCount - tradeCount, nextBadge.Emoji, nextBadge.TradeCount)
            : AppLocalization.Get(LocalizationKeys.DiscordProfileAllBadgesUnlocked);
        var nextTitle = nextBadge != null ? GetCurrentStatus(nextBadge.TradeCount) : AppLocalization.Get(LocalizationKeys.DiscordProfileMaxTitle);

        var badgesEmbed = new EmbedBuilder()
            .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordProfileBadgesTitle, targetUser.Username))
            .WithDescription(GetEarnedBadgesWithDescriptions(tradeCount))
            .WithColor(Color.Gold)
            .WithThumbnailUrl(targetUser.GetAvatarUrl() ?? targetUser.GetDefaultAvatarUrl())
            .AddField("\u200B", "\u200B")
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileNextBadge), nextBadgeInfo, true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordProfileNextTitle), nextTitle, true)
            .WithFooter(footer => footer.WithText(AppLocalization.Format(LocalizationKeys.DiscordProfileBadgesFooter, tradeCount))
                                        .WithIconUrl(Context.Guild?.IconUrl))
            .WithCurrentTimestamp()
            .Build();

        await message.ModifyAsync(msg =>
        {
            msg.Embed = badgesEmbed;
            msg.Components = BuildProfileComponents("back_to_profile");
        }).ConfigureAwait(false);
    }

    private async Task<SocketMessageComponent?> WaitForSelectMenuResponseAsync(IUserMessage message, ulong userId, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<SocketMessageComponent?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Context.Client.InteractionCreated += OnInteractionCreated;

        try
        {
            await using var _ = token.Register(() => tcs.TrySetResult(null));
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            Context.Client.InteractionCreated -= OnInteractionCreated;
        }

        Task OnInteractionCreated(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component &&
                component.User.Id == userId &&
                component.Message.Id == message.Id &&
                component.Data.CustomId == "profile_badges")
            {
                tcs.TrySetResult(component);
            }

            return Task.CompletedTask;
        }
    }

    private static string GetProgressBar(double percentage)
    {
        const int totalBlocks = 10;
        var filledBlocks = Math.Clamp((int)(percentage / 10), 0, totalBlocks);
        return $"[{new string('█', filledBlocks)}{new string('░', totalBlocks - filledBlocks)}] {percentage:0.00}%";
    }

    private static int GetRequiredXPForNextLevel(int currentLevel) => (int)(100 * Math.Pow(1.2, Math.Max(0, currentLevel - 1)));

    private static (int XP, int Level) GetGameStatsForUser(string userId)
    {
        if (!File.Exists(StatsFilePath))
            return (0, 1);

        try
        {
            var json = File.ReadAllText(StatsFilePath);
            var stats = JsonSerializer.Deserialize<Dictionary<string, UserStats>>(json);
            if (stats != null && stats.TryGetValue(userId, out var userStats))
                return (userStats.XP, Math.Max(1, userStats.Level));
        }
        catch
        {
            return (0, 1);
        }

        return (0, 1);
    }

    private static string GetBadgesForTradeCount(int tradeCount)
    {
        var badges = SysCordSettings.Settings.CustomBadgeEmojis
            .OrderBy(b => b.TradeCount)
            .Where(b => tradeCount >= b.TradeCount)
            .Select(b => b.Emoji);

        var badgeText = string.Join(" ", badges);
        return string.IsNullOrWhiteSpace(badgeText) ? AppLocalization.Get(LocalizationKeys.DiscordProfileNoBadges) : badgeText;
    }

    private static string GetLastTradeText(TradeCodeStorage.TradeCodeDetails? tradeDetails)
    {
        if (string.IsNullOrWhiteSpace(tradeDetails?.LastTrade))
            return AppLocalization.Get(LocalizationKeys.DiscordProfileNoLastTrade);

        if (tradeDetails.LastTradeAt is { } lastTradeAt)
            return AppLocalization.Format(LocalizationKeys.DiscordProfileLastTradeValue, tradeDetails.LastTrade, lastTradeAt.ToUnixTimeSeconds());

        return $"**{tradeDetails.LastTrade}**";
    }

    private static (string OT, string SID, string TID) GetTrainerInfo(ulong userId)
    {
        var tradeDetails = new TradeCodeStorage().GetTradeDetails(userId);
        return tradeDetails != null
            ? (tradeDetails.OT ?? "N/A", tradeDetails.SID.ToString(), tradeDetails.TID.ToString())
            : ("N/A", "N/A", "N/A");
    }

    private static string GetCurrentStatus(int totalTrades)
    {
        return totalTrades switch
        {
            >= 700 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonGod),
            >= 650 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonMaster),
            >= 600 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusWorldFamous),
            >= 550 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusTradeMaster),
            >= 500 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusRegionMaster),
            >= 450 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonLegend),
            >= 400 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonSage),
            >= 350 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonTrader),
            >= 300 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonElite),
            >= 250 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonHero),
            >= 200 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonChampion),
            >= 150 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonSpecialist),
            >= 100 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonProfessor),
            >= 50 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusNoviceTrainer),
            >= 1 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusNewbieTrainer),
            _ => AppLocalization.Get(LocalizationKeys.DiscordProfileNewTrainer)
        };
    }

    private static string? GetTradeCodeForUser(ulong userId)
    {
        var tradeDetails = new TradeCodeStorage().GetTradeDetails(userId);
        if (string.IsNullOrWhiteSpace(tradeDetails?.Code))
            return null;

        var code = tradeDetails.Code.PadLeft(8, '0');
        return code.Length >= 8 ? $"||{code[..4]} {code[4..]}||" : $"||{code}||";
    }

    private static string GetEarnedBadgesWithDescriptions(int tradeCount)
    {
        var earnedBadges = SysCordSettings.Settings.CustomBadgeEmojis
            .OrderBy(b => b.TradeCount)
            .Where(b => tradeCount >= b.TradeCount)
            .Select(b => b.TradeCount == 1
                ? AppLocalization.Format(LocalizationKeys.DiscordProfileBadgeSingular, b.Emoji, b.TradeCount)
                : AppLocalization.Format(LocalizationKeys.DiscordProfileBadgePlural, b.Emoji, b.TradeCount));

        var text = string.Join("\n", earnedBadges);
        return string.IsNullOrWhiteSpace(text) ? AppLocalization.Get(LocalizationKeys.DiscordProfileNoBadges) : text;
    }

    private static int GetTradeCountForUser(ulong userId) => new TradeCodeStorage().GetTradeCount(userId);

    private IUser? GetUser(ulong userId) =>
        Context.Guild?.GetUser(userId) ?? Context.Client.GetUser(userId);

    private static async Task DeleteAfterDelayAsync(IMessage message, TimeSpan delay)
    {
        try
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay).ConfigureAwait(false);
            await message.DeleteAsync().ConfigureAwait(false);
        }
        catch
        {
            // Ignore missing permissions or already-deleted messages.
        }
    }
}
