using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using DrawingImaging = System.Drawing.Imaging;
using DrawingText = System.Drawing.Text;

#pragma warning disable CA1416

namespace SysBot.Pokemon.Discord;

public class TradeLeaderboardModule : ModuleBase<SocketCommandContext>
{
    private const int EntriesPerPage = 7;
    private const int MaxLeaderboardEntries = 21;
    private const int PageCount = 3;
    private const int ImageWidth = 1000;
    private const int HeroImageHeight = 320;
    private const int CompactImageHeight = 118;
    private const int MaxComponentTextLength = 3900;
    private const string PreviousButtonId = "tradeleaderboard_previous";
    private const string NextButtonId = "tradeleaderboard_next";
    private static readonly int[] AchievementMilestones = [1, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700];
    private static readonly HttpClient Http = new();

    [Command("tradeleaderboard")]
    [Alias("tradelb", "toptrades", "tlb")]
    [Summary("Shows the global trade leaderboard.")]
    public async Task TradeLeaderboardAsync()
    {
        var leaderboard = BuildTradeLeaderboard(Context.User.Id);
        using var pageFiles = await BuildTradeLeaderboardPageAttachmentsAsync(leaderboard.Entries, Context.User.Id, 0).ConfigureAwait(false);
        var message = await Context.Channel.SendFilesAsync(
            pageFiles.Attachments,
            components: BuildTradeLeaderboardComponent(Context.User.Id, 1, pageFiles.Count, leaderboard.RequestingUserRank, leaderboard.IsRequestingUserInTopLimit),
            flags: MessageFlags.ComponentsV2).ConfigureAwait(false);

        _ = HandleTradeLeaderboardInteractionsAsync(message, Context.User.Id, leaderboard.Entries, leaderboard.RequestingUserRank, leaderboard.IsRequestingUserInTopLimit, TimeSpan.FromMinutes(2));
    }

    private TradeLeaderboardData BuildTradeLeaderboard(ulong requestingUserId)
    {
        var storage = new TradeCodeStorage();
        var tradeDetails = storage.GetAllTradeDetails();
        var cachedUsers = GetCachedGlobalUsers();
        var globalMemberCount = Math.Max(1, Context.Client.Guilds.Sum(g => g.MemberCount));
        var entries = new List<TradeLeaderboardEntry>();

        foreach (var (userId, user) in cachedUsers)
        {
            var tradeCount = tradeDetails.TryGetValue(userId, out var details) ? Math.Max(0, details.TradeCount) : 0;
            entries.Add(BuildEntry(userId, user, tradeCount));
        }

        foreach (var (userId, details) in tradeDetails)
        {
            if (cachedUsers.ContainsKey(userId))
                continue;

            var user = GetKnownGlobalUser(userId);
            if (user == null && userId != requestingUserId)
                continue;

            var displayUser = user ?? Context.User;
            entries.Add(BuildEntry(userId, displayUser, Math.Max(0, details.TradeCount)));
        }

        if (entries.All(e => e.UserId != requestingUserId))
            entries.Add(BuildEntry(requestingUserId, Context.User, storage.GetTradeCount(requestingUserId)));

        var ranked = entries
            .OrderByDescending(e => e.TradeCount)
            .ThenByDescending(e => e.CurrentMilestone)
            .ThenBy(e => e.UserId)
            .Select((entry, index) => entry with { Rank = index + 1 })
            .ToList();

        var requestingEntry = ranked.FirstOrDefault(e => e.UserId == requestingUserId);
        var requestingRank = requestingEntry?.TradeCount > 0 ? requestingEntry.Rank : globalMemberCount;
        var displayEntries = ranked
            .Where(e => e.TradeCount > 0)
            .Take(MaxLeaderboardEntries)
            .ToList();

        return new TradeLeaderboardData(
            displayEntries,
            Math.Min(Math.Max(1, requestingRank), globalMemberCount),
            displayEntries.Any(e => e.UserId == requestingUserId));
    }

    private static TradeLeaderboardEntry BuildEntry(ulong userId, IUser user, int tradeCount)
    {
        var currentMilestone = GetCurrentMilestone(tradeCount);
        var nextMilestone = GetNextMilestone(tradeCount);
        var medalMilestone = currentMilestone > 0 ? currentMilestone : (nextMilestone ?? 1);
        var palette = GetMedalPalette(medalMilestone);

        return new TradeLeaderboardEntry(
            userId,
            GetDisplayName(user),
            user.GetAvatarUrl(size: 256) ?? user.GetDefaultAvatarUrl(),
            tradeCount,
            currentMilestone,
            nextMilestone,
            GetTradeStatus(currentMilestone),
            palette,
            0);
    }

    private async Task<TradeLeaderboardPageFiles> BuildTradeLeaderboardPageAttachmentsAsync(IReadOnlyList<TradeLeaderboardEntry> entries, ulong requestingUserId, int pageIndex)
    {
        var pageEntries = entries
            .Skip(pageIndex * EntriesPerPage)
            .Take(EntriesPerPage)
            .ToList();
        if (pageEntries.Count == 0)
            pageEntries.Add(TradeLeaderboardEntry.Empty);

        var streams = new List<MemoryStream>();
        var attachments = new List<FileAttachment>();
        for (var i = 0; i < pageEntries.Count; i++)
        {
            var isHero = i == 0 && pageIndex == 0 && !pageEntries[i].IsEmpty;
            var stream = await BuildTradeLeaderboardCardImageAsync(pageEntries[i], isHero).ConfigureAwait(false);
            streams.Add(stream);
            attachments.Add(new FileAttachment(
                stream,
                GetCardFileName(requestingUserId, pageIndex, i),
                AppLocalization.Format(LocalizationKeys.DiscordTradeLeaderboardImageDescription, pageIndex + 1)));
        }

        return new TradeLeaderboardPageFiles(streams, attachments);
    }

    private async Task<MemoryStream> BuildTradeLeaderboardCardImageAsync(TradeLeaderboardEntry entry, bool hero)
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
                DrawEmptyTradeCard(graphics);
            else if (hero)
                await DrawHeroTradeEntryAsync(graphics, entry).ConfigureAwait(false);
            else
                await DrawCompactTradeEntryAsync(graphics, entry).ConfigureAwait(false);
        }

        var stream = new MemoryStream();
        bitmap.Save(stream, DrawingImaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    private static MessageComponent BuildTradeLeaderboardComponent(ulong requestingUserId, int page, int imageCount, int requestingUserRank, bool isRequestingUserInTopLimit, bool disableActions = false)
    {
        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(new Color(255, 204, 86));

        container.WithTextDisplay(TrimComponentText($"## 🏅 {AppLocalization.Get(LocalizationKeys.DiscordTradeLeaderboardTitle)}"));
        container.WithTextDisplay(TrimComponentText(AppLocalization.Format(LocalizationKeys.DiscordLeaderboardShowing, page, PageCount)));
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        for (var i = 0; i < imageCount; i++)
        {
            container.WithMediaGallery(new MediaGalleryBuilder()
                .AddItem($"attachment://{GetCardFileName(requestingUserId, page - 1, i)}", AppLocalization.Get(LocalizationKeys.DiscordTradeLeaderboardTitle), false));
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
                .WithCustomId($"tradeleaderboard_page_{page}")
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
            ? AppLocalization.Get(LocalizationKeys.DiscordTradeLeaderboardTopFooter)
            : AppLocalization.Format(LocalizationKeys.DiscordTradeLeaderboardUserRankFooter, $"#{requestingUserRank:N0}");
        container.WithTextDisplay(TrimComponentText($"{footer} • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f>"));

        builder.WithContainer(container);
        return builder.Build();
    }

    private async Task HandleTradeLeaderboardInteractionsAsync(IUserMessage message, ulong userId, IReadOnlyList<TradeLeaderboardEntry> entries, int requestingUserRank, bool isRequestingUserInTopLimit, TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var page = 1;
        var imageCount = Math.Min(EntriesPerPage, Math.Max(1, entries.Count));

        while (!timeoutCts.IsCancellationRequested)
        {
            var interaction = await WaitForTradeLeaderboardInteractionAsync(message, userId, timeoutCts.Token).ConfigureAwait(false);
            if (interaction == null)
                break;

            if (interaction.Data.CustomId == PreviousButtonId)
                page = Math.Max(1, page - 1);
            else if (interaction.Data.CustomId == NextButtonId)
                page = Math.Min(PageCount, page + 1);

            using var pageFiles = await BuildTradeLeaderboardPageAttachmentsAsync(entries, userId, page - 1).ConfigureAwait(false);
            imageCount = pageFiles.Count;
            await interaction.UpdateAsync(m =>
            {
                m.Attachments = new Optional<IEnumerable<FileAttachment>>(pageFiles.Attachments);
                m.Components = BuildTradeLeaderboardComponent(userId, page, imageCount, requestingUserRank, isRequestingUserInTopLimit);
            }).ConfigureAwait(false);
        }

        await message.ModifyAsync(m => m.Components = BuildTradeLeaderboardComponent(userId, page, imageCount, requestingUserRank, isRequestingUserInTopLimit, disableActions: true)).ConfigureAwait(false);
    }

    private async Task<SocketMessageComponent?> WaitForTradeLeaderboardInteractionAsync(IUserMessage message, ulong userId, CancellationToken token)
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

    private static async Task DrawHeroTradeEntryAsync(Drawing.Graphics graphics, TradeLeaderboardEntry entry)
    {
        var bounds = new Drawing.RectangleF(0, 0, ImageWidth, HeroImageHeight);
        DrawMedalBackground(graphics, bounds, entry.Palette, true, new Drawing.PointF(790, 160));
        DrawHeroMedalSparkles(graphics);
        await DrawAvatarAsync(graphics, entry.AvatarUrl, entry.DisplayName, new Drawing.RectangleF(58, 48, 158, 158), 7).ConfigureAwait(false);
        DrawMedal(graphics, entry, new Drawing.RectangleF(694, 22, 250, 250), true);

        using var rankFont = new Drawing.Font("Segoe UI", 86, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var nameFont = new Drawing.Font("Segoe UI", 35, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var titleFont = new Drawing.Font("Segoe UI", 20, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var whiteBrush = new Drawing.SolidBrush(Drawing.Color.White);
        using var softBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(238, 255, 246, 224));

        DrawTextWithShadow(graphics, $"#{entry.Rank:N0}", rankFont, whiteBrush, 250, 70);
        DrawTextWithShadow(graphics, TrimForCard(entry.DisplayName, 20), nameFont, whiteBrush, 52, 218);
        DrawTextWithShadow(graphics, TrimForCard(entry.Title, 30), titleFont, softBrush, 250, 166);
        DrawTradeProgressBar(graphics, entry, new Drawing.RectangleF(52, 266, 384, 30), true);
    }

    private static async Task DrawCompactTradeEntryAsync(Drawing.Graphics graphics, TradeLeaderboardEntry entry)
    {
        var bounds = new Drawing.RectangleF(0, 0, ImageWidth, CompactImageHeight);
        DrawMedalBackground(graphics, bounds, entry.Palette, false, new Drawing.PointF(760, 65));
        DrawCompactMedalSparkles(graphics);

        using var rankFont = new Drawing.Font("Segoe UI", 36, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var nameFont = new Drawing.Font("Segoe UI", 33, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var whiteBrush = new Drawing.SolidBrush(Drawing.Color.White);

        DrawTextWithShadow(graphics, $"#{entry.Rank:N0}", rankFont, whiteBrush, 18, 42);
        await DrawAvatarAsync(graphics, entry.AvatarUrl, entry.DisplayName, new Drawing.RectangleF(88, 8, 102, 102), 4).ConfigureAwait(false);
        DrawTextWithShadow(graphics, TrimForCard(entry.DisplayName, 22), nameFont, whiteBrush, 220, 18);
        DrawTradeProgressBar(graphics, entry, new Drawing.RectangleF(220, 70, 360, 26), false);
        DrawMedal(graphics, entry, new Drawing.RectangleF(704, -40, 250, 200), false);
    }

    private static void DrawEmptyTradeCard(Drawing.Graphics graphics)
    {
        var palette = GetMedalPalette(1);
        var bounds = new Drawing.RectangleF(0, 0, ImageWidth, HeroImageHeight);
        DrawMedalBackground(graphics, bounds, palette, true, new Drawing.PointF(500, 198));
        DrawEmptyMedalSparkles(graphics);

        using var titleFont = new Drawing.Font("Segoe UI", 32, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var titleBrush = new Drawing.SolidBrush(Drawing.Color.White);
        using var format = new Drawing.StringFormat
        {
            Alignment = Drawing.StringAlignment.Center,
            LineAlignment = Drawing.StringAlignment.Center
        };
        var titleBounds = new Drawing.RectangleF(92, 34, ImageWidth - 184, 76);
        DrawTextWithShadow(graphics, AppLocalization.Get(LocalizationKeys.DiscordTradeLeaderboardNotEnoughUsers), titleFont, titleBrush, titleBounds, format);
        DrawMedal(graphics, TradeLeaderboardEntry.Empty, new Drawing.RectangleF(392, 110, 215, 175), true);
    }

    private static void DrawMedalBackground(Drawing.Graphics graphics, Drawing.RectangleF bounds, MedalPalette palette, bool hero, Drawing.PointF rayOrigin)
    {
        using var path = CreateRoundedRectPath(bounds, 10);
        using var fill = new Drawing2D.LinearGradientBrush(bounds, palette.Dark, palette.Light, 0f);
        graphics.FillPath(fill, path);

        for (var i = 0; i < 14; i++)
        {
            var angle1 = Math.PI * 2 * i / 14;
            var angle2 = Math.PI * 2 * (i + 0.48) / 14;
            var p1 = new Drawing.PointF(rayOrigin.X + (float)Math.Cos(angle1) * bounds.Width, rayOrigin.Y + (float)Math.Sin(angle1) * bounds.Width);
            var p2 = new Drawing.PointF(rayOrigin.X + (float)Math.Cos(angle2) * bounds.Width, rayOrigin.Y + (float)Math.Sin(angle2) * bounds.Width);
            using var rayBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(hero ? 18 : 10, 255, 255, 255));
            graphics.FillPolygon(rayBrush, new[] { rayOrigin, p1, p2 });
        }

        using var panel = new Drawing.SolidBrush(Drawing.Color.FromArgb(hero ? 38 : 28, 255, 255, 255));
        graphics.FillRectangle(panel, 0, bounds.Height * 0.58f, bounds.Width, bounds.Height * 0.42f);

        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(hero ? 70 : 42, 255, 255, 255), 1);
        graphics.DrawPath(borderPen, path);
    }

    private static void DrawHeroMedalSparkles(Drawing.Graphics graphics)
    {
        DrawSpark(graphics, 660, 70, 23);
        DrawSpark(graphics, 950, 86, 25);
        DrawSpark(graphics, 684, 244, 20);
        DrawSpark(graphics, 922, 238, 22);
    }

    private static void DrawCompactMedalSparkles(Drawing.Graphics graphics)
    {
        DrawSpark(graphics, 676, 38, 16);
        DrawSpark(graphics, 935, 42, 20);
        DrawSpark(graphics, 636, 92, 14);
    }

    private static void DrawEmptyMedalSparkles(Drawing.Graphics graphics)
    {
        DrawSpark(graphics, 332, 208, 18);
        DrawSpark(graphics, 666, 196, 20);
        DrawSpark(graphics, 312, 116, 16);
        DrawSpark(graphics, 690, 116, 16);
    }

    private static void DrawTradeProgressBar(Drawing.Graphics graphics, TradeLeaderboardEntry entry, Drawing.RectangleF bounds, bool hero)
    {
        var target = entry.NextMilestone ?? Math.Max(entry.TradeCount, entry.CurrentMilestone);
        var previous = entry.CurrentMilestone;
        var rangeStart = previous <= 0 ? 0 : previous;
        var range = Math.Max(1, target - rangeStart);
        var progress = entry.NextMilestone == null ? 1f : Math.Clamp((entry.TradeCount - rangeStart) / (float)range, 0f, 1f);

        using var barBack = new Drawing.SolidBrush(Drawing.Color.FromArgb(hero ? 230 : 215, 24, 22, 28));
        FillRoundedRect(graphics, barBack, bounds, bounds.Height / 2);

        var fillWidth = Math.Max(bounds.Height, bounds.Width * progress);
        using var fillBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.RectangleF(bounds.X, bounds.Y, Math.Max(1, fillWidth), bounds.Height),
            Drawing.Color.FromArgb(255, 255, 248, 224),
            entry.Palette.Accent,
            0f);
        FillRoundedRect(graphics, fillBrush, new Drawing.RectangleF(bounds.X, bounds.Y, fillWidth, bounds.Height), bounds.Height / 2);

        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(235, 255, 255, 255), hero ? 3 : 2);
        DrawRoundedRect(graphics, borderPen, bounds, bounds.Height / 2);

        using var xpFont = new Drawing.Font("Segoe UI", hero ? 21 : 14, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var whiteBrush = new Drawing.SolidBrush(Drawing.Color.White);
        var label = entry.NextMilestone == null
            ? AppLocalization.Format(LocalizationKeys.DiscordTradeLeaderboardTradesValue, entry.TradeCount.ToString("N0"))
            : $"{entry.TradeCount:N0}/{entry.NextMilestone:N0} {AppLocalization.Get(LocalizationKeys.DiscordTradeLeaderboardTradesShort)}";
        DrawTextWithShadow(graphics, label, xpFont, whiteBrush, bounds.Right + 18, bounds.Y - (hero ? 2 : 1));
    }

    private static void DrawMedal(Drawing.Graphics graphics, TradeLeaderboardEntry entry, Drawing.RectangleF bounds, bool hero)
    {
        var milestone = entry.CurrentMilestone > 0 ? entry.CurrentMilestone : entry.NextMilestone ?? 1;
        var useSilhouette = entry.CurrentMilestone <= 0 || entry.IsEmpty;
        var assetPath = useSilhouette ? GetAchievementSilhouetteAssetPath(milestone) : GetAchievementMedalAssetPath(milestone);

        using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(58, 20, 14, 10));
        graphics.FillEllipse(shadowBrush, bounds.X + bounds.Width * 0.18f, bounds.Bottom - (hero ? 18 : 20), bounds.Width * 0.64f, hero ? 24 : 18);
        DrawMedalGlow(graphics, bounds, entry.Palette.Accent);

        if (!string.IsNullOrWhiteSpace(assetPath) && File.Exists(assetPath))
        {
            try
            {
                using var image = Drawing.Image.FromFile(assetPath);
                DrawImageGlow(graphics, image, bounds, LightenColor(entry.Palette.Accent, 0.25f));
                DrawImageContained(graphics, image, bounds);
                return;
            }
            catch
            {
                // Fall through to a drawn fallback if the medal image is unavailable.
            }
        }

        DrawMedalFallback(graphics, bounds, entry.Palette);
    }

    private static void DrawMedalGlow(Drawing.Graphics graphics, Drawing.RectangleF bounds, Drawing.Color glowColor)
    {
        var glowBounds = Drawing.RectangleF.Inflate(bounds, 34, 26);
        using var glowPath = CreateEllipsePath(glowBounds);
        using var glow = new Drawing2D.PathGradientBrush(glowPath)
        {
            CenterColor = Drawing.Color.FromArgb(104, LightenColor(glowColor, 0.3f)),
            SurroundColors = new[] { Drawing.Color.FromArgb(0, glowColor) }
        };
        graphics.FillEllipse(glow, glowBounds);
    }

    private static void DrawMedalFallback(Drawing.Graphics graphics, Drawing.RectangleF bounds, MedalPalette palette)
    {
        using var fill = new Drawing2D.LinearGradientBrush(bounds, LightenColor(palette.Accent, 0.34f), palette.Accent, 90f);
        graphics.FillEllipse(fill, bounds);
        using var pen = new Drawing.Pen(Drawing.Color.FromArgb(210, 255, 255, 255), Math.Max(4, bounds.Width / 28));
        graphics.DrawLine(pen, bounds.X + bounds.Width * 0.32f, bounds.Y + bounds.Height * 0.5f, bounds.X + bounds.Width * 0.68f, bounds.Y + bounds.Height * 0.5f);
        graphics.DrawLine(pen, bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.32f, bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.68f);
    }

    private static int GetCurrentMilestone(int tradeCount) =>
        AchievementMilestones.OrderByDescending(m => m).FirstOrDefault(m => tradeCount >= m);

    private static int? GetNextMilestone(int tradeCount) =>
        AchievementMilestones.OrderBy(m => m).FirstOrDefault(m => tradeCount < m) is var milestone && milestone > 0 ? milestone : null;

    private static string? GetAchievementMedalAssetPath(int milestone)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Medals", $"{milestone:000}.png");
        return File.Exists(path) ? path : null;
    }

    private static string? GetAchievementSilhouetteAssetPath(int milestone)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Medals Siluetas", $"Silueta_{milestone}.png");
        return File.Exists(path) ? path : null;
    }

    private static string GetTradeStatus(int milestone) =>
        milestone switch
        {
            1 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusNewbieTrainer),
            50 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusNoviceTrainer),
            100 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonProfessor),
            150 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonSpecialist),
            200 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonChampion),
            250 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonHero),
            300 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonElite),
            350 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonTrader),
            400 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonSage),
            450 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonLegend),
            500 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusRegionMaster),
            550 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusTradeMaster),
            600 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusWorldFamous),
            650 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonMaster),
            700 => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusPokemonGod),
            _ => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusNewTrainer),
        };

    private static MedalPalette GetMedalPalette(int milestone) =>
        milestone switch
        {
            >= 650 => new MedalPalette(Drawing.Color.FromArgb(255, 99, 49, 148), Drawing.Color.FromArgb(255, 244, 130, 234), Drawing.Color.FromArgb(255, 252, 120, 229)),
            >= 500 => new MedalPalette(Drawing.Color.FromArgb(255, 31, 105, 122), Drawing.Color.FromArgb(255, 117, 236, 224), Drawing.Color.FromArgb(255, 102, 230, 218)),
            >= 350 => new MedalPalette(Drawing.Color.FromArgb(255, 98, 42, 60), Drawing.Color.FromArgb(255, 242, 102, 139), Drawing.Color.FromArgb(255, 255, 89, 137)),
            >= 200 => new MedalPalette(Drawing.Color.FromArgb(255, 102, 58, 23), Drawing.Color.FromArgb(255, 248, 171, 70), Drawing.Color.FromArgb(255, 255, 185, 60)),
            >= 50 => new MedalPalette(Drawing.Color.FromArgb(255, 74, 78, 89), Drawing.Color.FromArgb(255, 209, 218, 229), Drawing.Color.FromArgb(255, 213, 222, 232)),
            _ => new MedalPalette(Drawing.Color.FromArgb(255, 92, 48, 34), Drawing.Color.FromArgb(255, 225, 139, 78), Drawing.Color.FromArgb(255, 239, 153, 82)),
        };

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

    private static string GetDisplayName(IUser user)
    {
        if (user is IGuildUser guildUser && !string.IsNullOrWhiteSpace(guildUser.Nickname))
            return guildUser.Nickname;

        if (!string.IsNullOrWhiteSpace(user.GlobalName))
            return user.GlobalName;

        return user.Username;
    }

    private static string GetCardFileName(ulong userId, int pageIndex, int slotIndex) =>
        $"tradeleaderboard-{userId}-page-{pageIndex + 1}-card-{slotIndex + 1}.png";

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

    private static async Task DrawAvatarAsync(Drawing.Graphics graphics, string avatarUrl, string displayName, Drawing.RectangleF bounds, int border)
    {
        using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(75, 20, 12, 8));
        graphics.FillEllipse(shadowBrush, bounds.X + 5, bounds.Y + 8, bounds.Width, bounds.Height);

        using var clipPath = CreateEllipsePath(bounds);
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
            using var fallback = new Drawing2D.LinearGradientBrush(bounds, Drawing.Color.FromArgb(255, 76, 54, 68), Drawing.Color.FromArgb(255, 239, 168, 76), 45f);
            graphics.FillEllipse(fallback, bounds);

            using var initialFont = new Drawing.Font("Segoe UI", bounds.Width * 0.43f, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
            using var initialBrush = new Drawing.SolidBrush(Drawing.Color.White);
            var initial = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim()[0].ToString().ToUpperInvariant();
            DrawCenteredText(graphics, initial, initialFont, initialBrush, bounds);
        }
        finally
        {
            graphics.Clip = previousClip;
        }

        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(245, 98, 57, 34), border);
        using var innerPen = new Drawing.Pen(Drawing.Color.FromArgb(215, 255, 245, 218), Math.Max(2, border / 2));
        graphics.DrawEllipse(borderPen, bounds);
        graphics.DrawEllipse(innerPen, Drawing.RectangleF.Inflate(bounds, -border, -border));
    }

    private static string TrimForCard(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
    }

    private sealed record TradeLeaderboardData(
        IReadOnlyList<TradeLeaderboardEntry> Entries,
        int RequestingUserRank,
        bool IsRequestingUserInTopLimit);

    private sealed class TradeLeaderboardPageFiles(IReadOnlyList<MemoryStream> streams, IReadOnlyList<FileAttachment> attachments) : IDisposable
    {
        public IReadOnlyList<FileAttachment> Attachments { get; } = attachments;

        public int Count => attachments.Count;

        public void Dispose()
        {
            foreach (var stream in streams)
                stream.Dispose();
        }
    }

    private sealed record TradeLeaderboardEntry(
        ulong UserId,
        string DisplayName,
        string AvatarUrl,
        int TradeCount,
        int CurrentMilestone,
        int? NextMilestone,
        string Title,
        MedalPalette Palette,
        int Rank)
    {
        public static TradeLeaderboardEntry Empty { get; } = new(
            0,
            string.Empty,
            string.Empty,
            0,
            0,
            1,
            AppLocalization.Get(LocalizationKeys.DiscordMedalStatusNewTrainer),
            GetMedalPalette(1),
            0);

        public bool IsEmpty => UserId == 0;
    }

    private sealed record MedalPalette(
        Drawing.Color Dark,
        Drawing.Color Light,
        Drawing.Color Accent);
}
