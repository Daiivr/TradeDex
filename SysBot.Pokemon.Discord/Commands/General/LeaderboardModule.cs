using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Pokemon.Discord.Models;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using DrawingImaging = System.Drawing.Imaging;
using DrawingText = System.Drawing.Text;

#pragma warning disable CA1416

namespace SysBot.Pokemon.Discord;

public class LeaderboardModule : ModuleBase<SocketCommandContext>
{
    private const string StatsFilePath = "user_stats.json";
    private const int EntriesPerPage = 7;
    private const int MaxLeaderboardEntries = 21;
    private const int PageCount = 3;
    private const int ImageWidth = 1000;
    private const int HeroImageHeight = 320;
    private const int CompactImageHeight = 118;
    private const int MaxComponentTextLength = 3900;
    private const string PreviousButtonId = "leaderboard_previous";
    private const string NextButtonId = "leaderboard_next";
    private static readonly HttpClient Http = new();

    [Command("leaderboard")]
    [Alias("lb", "ranking", "top")]
    [Summary("Shows the global XP leaderboard.")]
    public async Task LeaderboardAsync()
    {
        var leaderboard = await BuildLeaderboardAsync(Context.User.Id).ConfigureAwait(false);
        using (var pageFiles = await BuildLeaderboardPageAttachmentsAsync(leaderboard.Entries, Context.User.Id, 0).ConfigureAwait(false))
        {
            var message = await Context.Channel.SendFilesAsync(
                pageFiles.Attachments,
                components: BuildLeaderboardComponent(Context.User.Id, 1, pageFiles.Count, leaderboard.RequestingUserRank, leaderboard.IsRequestingUserInTopLimit),
                flags: MessageFlags.ComponentsV2).ConfigureAwait(false);

            _ = HandleLeaderboardInteractionsAsync(message, Context.User.Id, leaderboard.Entries, leaderboard.RequestingUserRank, leaderboard.IsRequestingUserInTopLimit, TimeSpan.FromMinutes(2));
        }
    }

    private async Task<LeaderboardData> BuildLeaderboardAsync(ulong requestingUserId)
    {
        var stats = ReadStats();
        if (!stats.ContainsKey(requestingUserId.ToString(CultureInfo.InvariantCulture)))
            stats[requestingUserId.ToString(CultureInfo.InvariantCulture)] = new UserStats { Level = 1, XP = 0 };

        var globalMemberCount = Math.Max(1, Context.Client.Guilds.Sum(g => g.MemberCount));
        var cachedUsers = GetCachedGlobalUsers();
        var knownUsers = new List<LeaderboardEntry>();

        foreach (var (userId, user) in cachedUsers)
        {
            var key = userId.ToString(CultureInfo.InvariantCulture);
            stats.TryGetValue(key, out var value);
            value ??= new UserStats { Level = 1, XP = 0 };

            var level = Math.Max(1, value.Level);
            var xp = Math.Max(0, value.XP);
            var league = GetLeagueForLevel(level);
            var displayName = GetDisplayName(user);
            var avatarUrl = user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl();
            knownUsers.Add(new LeaderboardEntry(userId, displayName, avatarUrl, level, xp, league, 0));
        }

        foreach (var (key, value) in stats)
        {
            if (!ulong.TryParse(key, out var userId) || value == null || cachedUsers.ContainsKey(userId))
                continue;

            var user = GetKnownGlobalUser(userId);
            if (user == null && userId != requestingUserId)
                continue;

            var level = Math.Max(1, value.Level);
            var xp = Math.Max(0, value.XP);
            var league = GetLeagueForLevel(level);
            var displayUser = user ?? Context.User;
            var displayName = userId == requestingUserId && user == null
                ? GetDisplayName(Context.User)
                : GetDisplayName(displayUser);
            var avatarUrl = displayUser.GetAvatarUrl(size: 256) ?? displayUser.GetDefaultAvatarUrl();
            knownUsers.Add(new LeaderboardEntry(userId, displayName, avatarUrl, level, xp, league, 0));
        }

        var sorted = knownUsers
            .OrderByDescending(e => e.League.RequiredLevel)
            .ThenByDescending(e => e.Level)
            .ThenByDescending(e => e.XP)
            .ThenBy(e => e.UserId)
            .ToList();

        var reservesFirstPlace = sorted.Count > 0 && sorted[0].League.RequiredLevel < GetTopLeagueRequiredLevel();
        var ranked = sorted
            .Select((entry, index) => entry with { Rank = reservesFirstPlace ? index + 2 : index + 1 })
            .ToList();

        var requestingEntry = ranked.FirstOrDefault(e => e.UserId == requestingUserId);
        var requestingRank = requestingEntry?.Rank ?? globalMemberCount;
        if (requestingEntry is { Level: <= 1, XP: <= 0 } || requestingRank <= 0)
            requestingRank = globalMemberCount;
        var displayEntries = ranked.Take(MaxLeaderboardEntries).ToList();

        return new LeaderboardData(
            displayEntries,
            Math.Min(requestingRank, globalMemberCount),
            displayEntries.Any(e => e.UserId == requestingUserId));
    }

    private async Task<LeaderboardPageFiles> BuildLeaderboardPageAttachmentsAsync(IReadOnlyList<LeaderboardEntry> entries, ulong requestingUserId, int pageIndex)
    {
        var pageEntries = entries
            .Skip(pageIndex * EntriesPerPage)
            .Take(EntriesPerPage)
            .ToList();
        if (pageEntries.Count == 0)
            pageEntries.Add(LeaderboardEntry.Empty);

        var streams = new List<MemoryStream>();
        var attachments = new List<FileAttachment>();
        for (var i = 0; i < pageEntries.Count; i++)
        {
            var isHero = i == 0 && pageIndex == 0 && !pageEntries[i].IsEmpty;
            var stream = await BuildLeaderboardCardImageAsync(pageEntries[i], isHero).ConfigureAwait(false);
            streams.Add(stream);
            attachments.Add(new FileAttachment(
                stream,
                GetCardFileName(requestingUserId, pageIndex, i),
                AppLocalization.Format(LocalizationKeys.DiscordLeaderboardImageDescription, pageIndex + 1)));
        }

        return new LeaderboardPageFiles(streams, attachments);
    }

    private async Task<MemoryStream> BuildLeaderboardCardImageAsync(LeaderboardEntry entry, bool hero)
    {
        var height = entry.IsEmpty ? HeroImageHeight : hero ? HeroImageHeight : CompactImageHeight;
        using var bitmap = new Drawing.Bitmap(ImageWidth, height, DrawingImaging.PixelFormat.Format32bppArgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

            if (entry.IsEmpty)
                DrawEmptyLeaderboardCard(graphics);
            else if (hero)
                await DrawHeroEntryAsync(graphics, entry).ConfigureAwait(false);
            else
                await DrawCompactEntryAsync(graphics, entry).ConfigureAwait(false);
        }

        var stream = new MemoryStream();
        bitmap.Save(stream, DrawingImaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    private static MessageComponent BuildLeaderboardComponent(ulong requestingUserId, int page, int imageCount, int requestingUserRank, bool isRequestingUserInTopLimit, bool disableActions = false)
    {
        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(new Color(255, 185, 75));

        container.WithTextDisplay(TrimComponentText($"## 🏆 {AppLocalization.Get(LocalizationKeys.DiscordLeaderboardTitle)}"));

        container.WithTextDisplay(TrimComponentText(AppLocalization.Format(LocalizationKeys.DiscordLeaderboardShowing, page, PageCount)));
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        for (var i = 0; i < imageCount; i++)
        {
            container.WithMediaGallery(new MediaGalleryBuilder()
                .AddItem($"attachment://{GetCardFileName(requestingUserId, page - 1, i)}", AppLocalization.Get(LocalizationKeys.DiscordLeaderboardTitle), false));
        }

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithActionRow([
            new ButtonBuilder()
                .WithLabel(AppLocalization.Get(LocalizationKeys.DiscordLeaderboardPrevious))
                .WithCustomId(PreviousButtonId)
                .WithStyle(ButtonStyle.Secondary)
                .WithDisabled(disableActions || page <= 1),
            new ButtonBuilder()
                .WithLabel(AppLocalization.Format(LocalizationKeys.DiscordLeaderboardPage, page, PageCount))
                .WithCustomId($"leaderboard_page_{page}")
                .WithStyle(ButtonStyle.Secondary)
                .WithDisabled(true),
            new ButtonBuilder()
                .WithLabel(AppLocalization.Get(LocalizationKeys.DiscordLeaderboardNext))
                .WithCustomId(NextButtonId)
                .WithStyle(ButtonStyle.Secondary)
                .WithDisabled(disableActions || page >= PageCount),
        ]);

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        var footer = isRequestingUserInTopLimit
            ? AppLocalization.Get(LocalizationKeys.DiscordLeaderboardTopFooter)
            : AppLocalization.Format(LocalizationKeys.DiscordLeaderboardUserRankFooter, $"#{requestingUserRank:N0}");
        container.WithTextDisplay(TrimComponentText($"{footer} • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f>"));

        builder.WithContainer(container);
        return builder.Build();
    }

    private async Task HandleLeaderboardInteractionsAsync(IUserMessage message, ulong userId, IReadOnlyList<LeaderboardEntry> entries, int requestingUserRank, bool isRequestingUserInTopLimit, TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var page = 1;
        var imageCount = Math.Min(EntriesPerPage, entries.Count);

        while (!timeoutCts.IsCancellationRequested)
        {
            var interaction = await WaitForLeaderboardInteractionAsync(message, userId, timeoutCts.Token).ConfigureAwait(false);
            if (interaction == null)
                break;

            if (interaction.Data.CustomId == PreviousButtonId)
                page = Math.Max(1, page - 1);
            else if (interaction.Data.CustomId == NextButtonId)
                page = Math.Min(PageCount, page + 1);

            using var pageFiles = await BuildLeaderboardPageAttachmentsAsync(entries, userId, page - 1).ConfigureAwait(false);
            imageCount = pageFiles.Count;
            await interaction.UpdateAsync(m =>
            {
                m.Attachments = new Optional<IEnumerable<FileAttachment>>(pageFiles.Attachments);
                m.Components = BuildLeaderboardComponent(userId, page, imageCount, requestingUserRank, isRequestingUserInTopLimit);
            }).ConfigureAwait(false);
        }

        await message.ModifyAsync(m => m.Components = BuildLeaderboardComponent(userId, page, imageCount, requestingUserRank, isRequestingUserInTopLimit, disableActions: true)).ConfigureAwait(false);
    }

    private async Task<SocketMessageComponent?> WaitForLeaderboardInteractionAsync(IUserMessage message, ulong userId, CancellationToken token)
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
                (component.Data.CustomId == PreviousButtonId || component.Data.CustomId == NextButtonId))
            {
                tcs.TrySetResult(component);
            }

            return Task.CompletedTask;
        }
    }

    private static async Task DrawHeroEntryAsync(Drawing.Graphics graphics, LeaderboardEntry entry)
    {
        var bounds = new Drawing.RectangleF(0, 0, ImageWidth, HeroImageHeight);
        DrawEntryBackground(graphics, bounds, entry.League, true);
        DrawHeroCardSparkles(graphics);

        await DrawAvatarAsync(graphics, entry.AvatarUrl, entry.DisplayName, new Drawing.RectangleF(58, 48, 158, 158), 7).ConfigureAwait(false);
        DrawTrophy(graphics, entry.League, new Drawing.RectangleF(690, 12, 252, 288), true);

        using var rankFont = new Drawing.Font("Segoe UI", 88, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var nameFont = new Drawing.Font("Segoe UI", 35, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var xpFont = new Drawing.Font("Segoe UI", 20, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var whiteBrush = new Drawing.SolidBrush(Drawing.Color.White);
        using var softBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(238, 255, 244, 220));

        DrawTextWithShadow(graphics, $"#{entry.Rank:N0}", rankFont, whiteBrush, 250, 72);
        DrawTextWithShadow(graphics, TrimForCard(entry.DisplayName, 20), nameFont, whiteBrush, 52, 218);
        DrawTextWithShadow(graphics, $"{AppLocalization.Get(LocalizationKeys.DiscordProfileLevel)} {entry.Level} • {entry.League.DisplayName}", xpFont, softBrush, 250, 168);
        DrawProgressBar(graphics, entry, new Drawing.RectangleF(52, 266, 384, 30), hero: true);
    }

    private static async Task DrawCompactEntryAsync(Drawing.Graphics graphics, LeaderboardEntry entry)
    {
        var bounds = new Drawing.RectangleF(0, 0, ImageWidth, CompactImageHeight);
        DrawEntryBackground(graphics, bounds, entry.League, false);
        DrawLeagueSparkles(graphics, bounds, 0.62f);

        using var rankFont = new Drawing.Font("Segoe UI", 36, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var nameFont = new Drawing.Font("Segoe UI", 33, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var whiteBrush = new Drawing.SolidBrush(Drawing.Color.White);

        DrawTextWithShadow(graphics, $"#{entry.Rank:N0}", rankFont, whiteBrush, 18, 42);
        await DrawAvatarAsync(graphics, entry.AvatarUrl, entry.DisplayName, new Drawing.RectangleF(88, 8, 102, 102), 4).ConfigureAwait(false);
        DrawTextWithShadow(graphics, TrimForCard(entry.DisplayName, 22), nameFont, whiteBrush, 220, 18);
        DrawProgressBar(graphics, entry, new Drawing.RectangleF(220, 70, 360, 26), hero: false);
        DrawTrophy(graphics, entry.League, new Drawing.RectangleF(710, 10, 220, 172), false);
    }

    private static void DrawEmptyLeaderboardCard(Drawing.Graphics graphics)
    {
        var league = GetLeagueForLevel(1);
        var bounds = new Drawing.RectangleF(0, 0, ImageWidth, HeroImageHeight);
        DrawEntryBackground(graphics, bounds, league, true, new Drawing.PointF(500, 198));
        DrawEmptyCardSparkles(graphics);

        using var titleFont = new Drawing.Font("Segoe UI", 32, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var titleBrush = new Drawing.SolidBrush(Drawing.Color.White);
        using var format = new Drawing.StringFormat
        {
            Alignment = Drawing.StringAlignment.Center,
            LineAlignment = Drawing.StringAlignment.Center
        };
        var titleBounds = new Drawing.RectangleF(80, 34, ImageWidth - 160, 76);
        DrawTextWithShadow(graphics, AppLocalization.Get(LocalizationKeys.DiscordLeaderboardNotEnoughUsers), titleFont, titleBrush, titleBounds, format);

        DrawTrophy(graphics, league, new Drawing.RectangleF(392, 104, 215, 185), true);
    }

    private static void DrawEntryBackground(Drawing.Graphics graphics, Drawing.RectangleF bounds, ProfileLeague league, bool hero, Drawing.PointF? rayOriginOverride = null)
    {
        using var path = CreateRoundedRectPath(bounds, 10);
        var leftColor = MixColors(league.Secondary, Drawing.Color.FromArgb(80, 41, 22), 0.52f);
        var rightColor = MixColors(LightenColor(league.Primary, hero ? 0.36f : 0.24f), Drawing.Color.FromArgb(42, 40, 52), hero ? 0.04f : 0.32f);
        using var fill = new Drawing2D.LinearGradientBrush(bounds, leftColor, rightColor, 0f);
        graphics.FillPath(fill, path);

        var origin = rayOriginOverride ?? new Drawing.PointF(bounds.X + bounds.Width * 0.12f, bounds.Y + bounds.Height * 0.58f);
        for (var i = 0; i < 12; i++)
        {
            var angle1 = Math.PI * 2 * i / 12;
            var angle2 = Math.PI * 2 * (i + 0.48) / 12;
            var p1 = new Drawing.PointF(origin.X + (float)Math.Cos(angle1) * bounds.Width, origin.Y + (float)Math.Sin(angle1) * bounds.Width);
            var p2 = new Drawing.PointF(origin.X + (float)Math.Cos(angle2) * bounds.Width, origin.Y + (float)Math.Sin(angle2) * bounds.Width);
            using var rayBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(hero ? 18 : 10, 255, 255, 255));
            graphics.FillPolygon(rayBrush, new[] { origin, p1, p2 });
        }

        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(hero ? 70 : 42, 255, 255, 255), 1);
        graphics.DrawPath(borderPen, path);
    }

    private static void DrawLeagueSparkles(Drawing.Graphics graphics, Drawing.RectangleF bounds, float scale)
    {
        DrawSpark(graphics, bounds.Right - 268, bounds.Y + 50, 22 * scale);
        DrawSpark(graphics, bounds.Right - 56, bounds.Y + 78, 24 * scale);
        DrawSpark(graphics, bounds.Right - 220, bounds.Bottom - 42, 20 * scale);
    }

    private static void DrawHeroCardSparkles(Drawing.Graphics graphics)
    {
        DrawSpark(graphics, 660, 74, 24);
        DrawSpark(graphics, 950, 78, 24);
        DrawSpark(graphics, 694, 245, 22);
        DrawSpark(graphics, 922, 246, 21);
    }

    private static void DrawEmptyCardSparkles(Drawing.Graphics graphics)
    {
        DrawSpark(graphics, 332, 208, 18);
        DrawSpark(graphics, 666, 196, 20);
        DrawSpark(graphics, 312, 116, 16);
        DrawSpark(graphics, 690, 116, 16);
    }

    private static void DrawProgressBar(Drawing.Graphics graphics, LeaderboardEntry entry, Drawing.RectangleF bounds, bool hero)
    {
        var requiredXp = XpProgression.GetRequiredXPForNextLevel(entry.Level);
        var progress = requiredXp <= 0 ? 0 : Math.Clamp((float)entry.XP / requiredXp, 0f, 1f);

        using var barBack = new Drawing.SolidBrush(Drawing.Color.FromArgb(hero ? 235 : 220, 29, 28, 31));
        FillRoundedRect(graphics, barBack, bounds, bounds.Height / 2);

        var fillWidth = Math.Max(bounds.Height, bounds.Width * progress);
        using var fillBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.RectangleF(bounds.X, bounds.Y, Math.Max(1, fillWidth), bounds.Height),
            Drawing.Color.FromArgb(255, 255, 250, 236),
            LightenColor(entry.League.Primary, 0.12f),
            0f);
        FillRoundedRect(graphics, fillBrush, new Drawing.RectangleF(bounds.X, bounds.Y, fillWidth, bounds.Height), bounds.Height / 2);

        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(235, 255, 255, 255), hero ? 3 : 2);
        DrawRoundedRect(graphics, borderPen, bounds, bounds.Height / 2);

        using var xpFont = new Drawing.Font("Segoe UI", hero ? 21 : 14, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var whiteBrush = new Drawing.SolidBrush(Drawing.Color.White);
        DrawTextWithShadow(graphics, $"{entry.XP:N0}/{requiredXp:N0} XP", xpFont, whiteBrush, bounds.Right + 18, bounds.Y - (hero ? 2 : 1));
    }

    private static async Task DrawAvatarAsync(Drawing.Graphics graphics, string avatarUrl, string displayName, Drawing.RectangleF bounds, int border, bool roundAvatar = true)
    {
        using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(75, 20, 12, 8));
        if (roundAvatar)
            graphics.FillEllipse(shadowBrush, bounds.X + 5, bounds.Y + 8, bounds.Width, bounds.Height);
        else
            FillRoundedRect(graphics, shadowBrush, new Drawing.RectangleF(bounds.X + 5, bounds.Y + 8, bounds.Width, bounds.Height), 18);

        using var clipPath = roundAvatar ? CreateEllipsePath(bounds) : CreateRoundedRectPath(bounds, 18);
        var previousClip = graphics.Clip;
        graphics.SetClip(clipPath);

        try
        {
            await using var stream = await Http.GetStreamAsync(avatarUrl).ConfigureAwait(false);
            using var avatar = Drawing.Image.FromStream(stream);
            DrawImageCovered(graphics, avatar, bounds);
        }
        catch
        {
            using var fallback = new Drawing2D.LinearGradientBrush(bounds, Drawing.Color.FromArgb(255, 91, 55, 78), Drawing.Color.FromArgb(255, 236, 167, 75), 45f);
            if (roundAvatar)
                graphics.FillEllipse(fallback, bounds);
            else
                FillRoundedRect(graphics, fallback, bounds, 18);

            using var initialFont = new Drawing.Font("Segoe UI", bounds.Width * 0.43f, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
            using var initialBrush = new Drawing.SolidBrush(Drawing.Color.White);
            var initial = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim()[0].ToString().ToUpperInvariant();
            DrawCenteredText(graphics, initial, initialFont, initialBrush, bounds);
        }
        finally
        {
            graphics.Clip = previousClip;
        }

        if (border <= 0)
            return;

        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(245, 95, 56, 30), border);
        using var innerPen = new Drawing.Pen(Drawing.Color.FromArgb(215, 255, 245, 218), Math.Max(2, border / 2));
        if (roundAvatar)
        {
            graphics.DrawEllipse(borderPen, bounds);
            graphics.DrawEllipse(innerPen, Drawing.RectangleF.Inflate(bounds, -border, -border));
        }
        else
        {
            DrawRoundedRect(graphics, borderPen, bounds, 18);
            DrawRoundedRect(graphics, innerPen, Drawing.RectangleF.Inflate(bounds, -border, -border), 14);
        }
    }

    private static void DrawTrophy(Drawing.Graphics graphics, ProfileLeague league, Drawing.RectangleF bounds, bool hero)
    {
        if (TryGetLeagueAssetPath(league.Key, out var assetPath))
        {
            try
            {
                using var image = Drawing.Image.FromFile(assetPath);
                using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(60, 48, 25, 12));
                graphics.FillEllipse(shadowBrush, bounds.X + bounds.Width * 0.14f, bounds.Bottom - (hero ? 24 : 18), bounds.Width * 0.7f, hero ? 26 : 20);
                DrawTrophyGlow(graphics, league, bounds);
                DrawImageGlow(graphics, image, bounds, LightenColor(league.Primary, 0.35f));
                DrawImageContained(graphics, image, bounds);
                return;
            }
            catch
            {
                // Fall through to emoji/vector fallback.
            }
        }

        using var font = new Drawing.Font("Segoe UI Emoji", bounds.Height * 0.42f, Drawing.FontStyle.Regular, Drawing.GraphicsUnit.Pixel);
        using var brush = new Drawing.SolidBrush(Drawing.Color.White);
        DrawCenteredText(graphics, league.Emoji, font, brush, bounds);
    }

    private static void DrawTrophyGlow(Drawing.Graphics graphics, ProfileLeague league, Drawing.RectangleF bounds)
    {
        var glowBounds = Drawing.RectangleF.Inflate(bounds, 34, 26);
        using var glowPath = CreateEllipsePath(glowBounds);
        using var glow = new Drawing2D.PathGradientBrush(glowPath)
        {
            CenterColor = Drawing.Color.FromArgb(112, LightenColor(league.Primary, 0.42f)),
            SurroundColors = new[] { Drawing.Color.FromArgb(0, league.Primary) }
        };
        graphics.FillEllipse(glow, glowBounds);
    }

    private Dictionary<string, UserStats> ReadStats()
    {
        if (!File.Exists(StatsFilePath))
            return new Dictionary<string, UserStats>();

        try
        {
            var json = File.ReadAllText(StatsFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, UserStats>>(json) ?? new Dictionary<string, UserStats>();
        }
        catch
        {
            return new Dictionary<string, UserStats>();
        }
    }

    private IUser? GetKnownGlobalUser(ulong userId)
    {
        foreach (var guild in Context.Client.Guilds)
        {
            var guildUser = guild.GetUser(userId);
            if (guildUser != null)
                return guildUser;
        }

        return Context.Client.GetUser(userId);
    }

    private Dictionary<ulong, IUser> GetCachedGlobalUsers()
    {
        var users = new Dictionary<ulong, IUser>();
        var guilds = Context.Client.Guilds
            .OrderByDescending(g => Context.Guild != null && g.Id == Context.Guild.Id)
            .ToList();

        foreach (var guild in guilds)
        {
            foreach (var user in guild.Users)
            {
                if (user.IsBot)
                    continue;

                users.TryAdd(user.Id, user);
            }
        }

        users.TryAdd(Context.User.Id, Context.User);
        return users;
    }

    private static ProfileLeague GetLeagueForLevel(int level)
    {
        var leagues = SysCordSettings.Settings.LeagueEmojis
            .OrderBy(l => l.RequiredLevel)
            .ToList();
        var selected = leagues.LastOrDefault(l => level >= l.RequiredLevel)
            ?? leagues.FirstOrDefault()
            ?? new LeagueEmoji("Bronze", 1, "🥉");
        var key = NormalizeLeagueKey(selected.Key);
        var (primary, secondary, isCup) = GetLeagueVisuals(key);

        return new ProfileLeague(
            key,
            string.IsNullOrWhiteSpace(selected.Emoji) ? "🥉" : selected.Emoji.Trim(),
            GetLeagueDisplayName(key, selected.Key),
            Math.Max(1, selected.RequiredLevel),
            primary,
            secondary,
            isCup);
    }

    private static string NormalizeLeagueKey(string? key)
    {
        var value = key?.Trim() ?? string.Empty;
        if (value.Contains("master", StringComparison.OrdinalIgnoreCase))
            return "Master";
        if (value.Contains("diamond", StringComparison.OrdinalIgnoreCase) || value.Contains("diamante", StringComparison.OrdinalIgnoreCase))
            return "Diamond";
        if (value.Contains("ruby", StringComparison.OrdinalIgnoreCase) || value.Contains("rubi", StringComparison.OrdinalIgnoreCase))
            return "Ruby";
        if (value.Contains("pearl", StringComparison.OrdinalIgnoreCase) || value.Contains("perla", StringComparison.OrdinalIgnoreCase))
            return "Pearl";
        if (value.Contains("gold", StringComparison.OrdinalIgnoreCase) || value.Contains("oro", StringComparison.OrdinalIgnoreCase))
            return "Gold";
        if (value.Contains("silver", StringComparison.OrdinalIgnoreCase) || value.Contains("plata", StringComparison.OrdinalIgnoreCase))
            return "Silver";
        return "Bronze";
    }

    private static string GetLeagueDisplayName(string key, string fallback) =>
        key switch
        {
            "Master" => AppLocalization.Get(LocalizationKeys.DiscordProfileLeagueMaster),
            "Diamond" => AppLocalization.Get(LocalizationKeys.DiscordProfileLeagueDiamond),
            "Ruby" => AppLocalization.Get(LocalizationKeys.DiscordProfileLeagueRuby),
            "Pearl" => AppLocalization.Get(LocalizationKeys.DiscordProfileLeaguePearl),
            "Gold" => AppLocalization.Get(LocalizationKeys.DiscordProfileLeagueGold),
            "Silver" => AppLocalization.Get(LocalizationKeys.DiscordProfileLeagueSilver),
            "Bronze" => AppLocalization.Get(LocalizationKeys.DiscordProfileLeagueBronze),
            _ => string.IsNullOrWhiteSpace(fallback) ? AppLocalization.Get(LocalizationKeys.DiscordProfileLeagueBronze) : fallback.Trim(),
        };

    private static (Drawing.Color Primary, Drawing.Color Secondary, bool IsCup) GetLeagueVisuals(string key) =>
        key switch
        {
            "Master" => (Drawing.Color.FromArgb(255, 202, 94, 245), Drawing.Color.FromArgb(255, 117, 71, 203), true),
            "Diamond" => (Drawing.Color.FromArgb(255, 104, 227, 255), Drawing.Color.FromArgb(255, 62, 146, 219), true),
            "Ruby" => (Drawing.Color.FromArgb(255, 255, 49, 90), Drawing.Color.FromArgb(255, 176, 18, 55), false),
            "Pearl" => (Drawing.Color.FromArgb(255, 255, 92, 173), Drawing.Color.FromArgb(255, 192, 35, 115), false),
            "Gold" => (Drawing.Color.FromArgb(255, 255, 190, 36), Drawing.Color.FromArgb(255, 191, 121, 19), false),
            "Silver" => (Drawing.Color.FromArgb(255, 225, 229, 232), Drawing.Color.FromArgb(255, 142, 151, 160), false),
            _ => (Drawing.Color.FromArgb(255, 223, 129, 74), Drawing.Color.FromArgb(255, 149, 82, 45), false),
        };

    private static int GetTopLeagueRequiredLevel() =>
        SysCordSettings.Settings.LeagueEmojis.Count == 0
            ? 1
            : SysCordSettings.Settings.LeagueEmojis.Max(l => Math.Max(1, l.RequiredLevel));

    private static bool TryGetLeagueAssetPath(string key, out string path)
    {
        var fileName = key switch
        {
            "Master" => "Champion_League.png",
            "Diamond" => "Diamond_League.png",
            "Ruby" => "Rubi_League.png",
            "Pearl" => "Pearl_League.png",
            "Gold" => "Gold_League.png",
            "Silver" => "Silver_League.png",
            _ => "Bronze_League.png",
        };

        path = Path.Combine(AppContext.BaseDirectory, "Assets", "Leagues", fileName);
        return File.Exists(path);
    }

    private static string GetDisplayName(IUser user)
    {
        if (user is IGuildUser guildUser && !string.IsNullOrWhiteSpace(guildUser.Nickname))
            return guildUser.Nickname;

        if (!string.IsNullOrWhiteSpace(user.GlobalName))
            return user.GlobalName;

        return user.Username;
    }

    private static string GetCardFileName(ulong userId, int pageIndex, int slotIndex) =>
        $"leaderboard-{userId}-page-{pageIndex + 1}-card-{slotIndex + 1}.png";

    private static string TrimComponentText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "\u200B";

        return text.Length <= MaxComponentTextLength ? text : text[..(MaxComponentTextLength - 3)] + "...";
    }

    private static void DrawImageCovered(Drawing.Graphics graphics, Drawing.Image image, Drawing.RectangleF bounds)
    {
        var scale = Math.Max(bounds.Width / image.Width, bounds.Height / image.Height);
        var width = image.Width * scale;
        var height = image.Height * scale;
        var x = bounds.X + (bounds.Width - width) / 2;
        var y = bounds.Y + (bounds.Height - height) / 2;
        graphics.DrawImage(image, x, y, width, height);
    }

    private static void DrawImageContained(Drawing.Graphics graphics, Drawing.Image image, Drawing.RectangleF bounds)
    {
        var imageBounds = GetContainedImageBounds(image, bounds);
        graphics.DrawImage(image, imageBounds);
    }

    private static void DrawImageGlow(Drawing.Graphics graphics, Drawing.Image image, Drawing.RectangleF bounds, Drawing.Color glowColor)
    {
        DrawTintedImage(graphics, image, GetContainedImageBounds(image, Drawing.RectangleF.Inflate(bounds, 28, 22)), glowColor, 0.10f);
        DrawTintedImage(graphics, image, GetContainedImageBounds(image, Drawing.RectangleF.Inflate(bounds, 14, 12)), glowColor, 0.20f);
    }

    private static void DrawTintedImage(Drawing.Graphics graphics, Drawing.Image image, Drawing.RectangleF bounds, Drawing.Color color, float opacity)
    {
        using var attributes = new DrawingImaging.ImageAttributes();
        var colorMatrix = new DrawingImaging.ColorMatrix(new[]
        {
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, 0, 0 },
            new float[] { 0, 0, 0, opacity, 0 },
            new[] { color.R / 255f, color.G / 255f, color.B / 255f, 0, 1 },
        });
        attributes.SetColorMatrix(colorMatrix, DrawingImaging.ColorMatrixFlag.Default, DrawingImaging.ColorAdjustType.Bitmap);

        var destination = Drawing.Rectangle.Round(bounds);
        graphics.DrawImage(image, destination, 0, 0, image.Width, image.Height, Drawing.GraphicsUnit.Pixel, attributes);
    }

    private static Drawing.RectangleF GetContainedImageBounds(Drawing.Image image, Drawing.RectangleF bounds)
    {
        var scale = Math.Min(bounds.Width / image.Width, bounds.Height / image.Height);
        var width = image.Width * scale;
        var height = image.Height * scale;
        var x = bounds.X + (bounds.Width - width) / 2;
        var y = bounds.Y + (bounds.Height - height) / 2;
        return new Drawing.RectangleF(x, y, width, height);
    }

    private static void DrawSpark(Drawing.Graphics graphics, float x, float y, float size)
    {
        using var pen = new Drawing.Pen(Drawing.Color.FromArgb(230, 255, 255, 255), Math.Max(4, size * 0.28f))
        {
            StartCap = Drawing2D.LineCap.Round,
            EndCap = Drawing2D.LineCap.Round
        };
        graphics.DrawLine(pen, x - size / 2, y, x + size / 2, y);
        graphics.DrawLine(pen, x, y - size / 2, x, y + size / 2);
    }

    private static void DrawTextWithShadow(Drawing.Graphics graphics, string text, Drawing.Font font, Drawing.Brush brush, float x, float y)
    {
        using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(84, 45, 22, 12));
        graphics.DrawString(text, font, shadowBrush, x + 3, y + 4);
        graphics.DrawString(text, font, brush, x, y);
    }

    private static void DrawTextWithShadow(Drawing.Graphics graphics, string text, Drawing.Font font, Drawing.Brush brush, Drawing.RectangleF bounds, Drawing.StringFormat format)
    {
        using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(84, 45, 22, 12));
        var shadowBounds = bounds;
        shadowBounds.Offset(3, 4);
        graphics.DrawString(text, font, shadowBrush, shadowBounds, format);
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static void DrawCenteredText(Drawing.Graphics graphics, string text, Drawing.Font font, Drawing.Brush brush, Drawing.RectangleF bounds)
    {
        using var format = new Drawing.StringFormat
        {
            Alignment = Drawing.StringAlignment.Center,
            LineAlignment = Drawing.StringAlignment.Center
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static Drawing.Color MixColors(Drawing.Color first, Drawing.Color second, float secondAmount)
    {
        secondAmount = Math.Clamp(secondAmount, 0f, 1f);
        var firstAmount = 1f - secondAmount;
        return Drawing.Color.FromArgb(
            255,
            (int)(first.R * firstAmount + second.R * secondAmount),
            (int)(first.G * firstAmount + second.G * secondAmount),
            (int)(first.B * firstAmount + second.B * secondAmount));
    }

    private static Drawing.Color LightenColor(Drawing.Color color, float amount) =>
        MixColors(color, Drawing.Color.White, amount);

    private static void FillRoundedRect(Drawing.Graphics graphics, Drawing.Brush brush, Drawing.RectangleF bounds, float radius)
    {
        using var path = CreateRoundedRectPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    private static void DrawRoundedRect(Drawing.Graphics graphics, Drawing.Pen pen, Drawing.RectangleF bounds, float radius)
    {
        using var path = CreateRoundedRectPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static Drawing2D.GraphicsPath CreateRoundedRectPath(Drawing.RectangleF bounds, float radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var arc = new Drawing.RectangleF(bounds.Location, new Drawing.SizeF(diameter, diameter));
        var path = new Drawing2D.GraphicsPath();

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }

    private static Drawing2D.GraphicsPath CreateEllipsePath(Drawing.RectangleF bounds)
    {
        var path = new Drawing2D.GraphicsPath();
        path.AddEllipse(bounds);
        return path;
    }

    private static string TrimForCard(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
    }

    private sealed record LeaderboardData(
        IReadOnlyList<LeaderboardEntry> Entries,
        int RequestingUserRank,
        bool IsRequestingUserInTopLimit);

    private sealed class LeaderboardPageFiles(IReadOnlyList<MemoryStream> streams, IReadOnlyList<FileAttachment> attachments) : IDisposable
    {
        public IReadOnlyList<FileAttachment> Attachments { get; } = attachments;

        public int Count => attachments.Count;

        public void Dispose()
        {
            foreach (var stream in streams)
                stream.Dispose();
        }
    }

    private sealed record LeaderboardEntry(
        ulong UserId,
        string DisplayName,
        string AvatarUrl,
        int Level,
        int XP,
        ProfileLeague League,
        int Rank)
    {
        public static LeaderboardEntry Empty { get; } = new(
            0,
            string.Empty,
            string.Empty,
            1,
            0,
            GetLeagueForLevel(1),
            0);

        public bool IsEmpty => UserId == 0;
    }

    private sealed record ProfileLeague(
        string Key,
        string Emoji,
        string DisplayName,
        int RequiredLevel,
        Drawing.Color Primary,
        Drawing.Color Secondary,
        bool IsCup);
}
