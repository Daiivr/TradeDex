using Discord;
using Discord.WebSocket;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using DrawingImaging = System.Drawing.Imaging;
using DrawingText = System.Drawing.Text;

namespace SysBot.Pokemon.Discord;

#pragma warning disable CA1416

public static class MedalHelpers
{
    private const int MaxComponentTextLength = 3900;
    private const int AchievementCollectionWidth = 1000;
    private const int AchievementCollectionHeight = 620;
    private static readonly Color MedalColor = new(255, 215, 0);
    private static readonly int[] MilestonesAscending = [1, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700];

    private static readonly Dictionary<int, string> MilestoneImages = new()
    {
        { 1, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/001.png" },
        { 50, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/050.png" },
        { 100, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/100.png" },
        { 150, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/150.png" },
        { 200, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/200.png" },
        { 250, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/250.png" },
        { 300, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/300.png" },
        { 350, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/350.png" },
        { 400, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/400.png" },
        { 450, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/450.png" },
        { 500, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/500.png" },
        { 550, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/550.png" },
        { 600, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/600.png" },
        { 650, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/650.png" },
        { 700, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/700.png" },
    };

    public static int GetCurrentMilestone(int totalTrades)
    {
        return MilestonesAscending.Reverse().FirstOrDefault(m => totalTrades >= m, 0);
    }

    public static bool TryGetMilestoneImageUrl(int milestone, out string imageUrl) =>
        MilestoneImages.TryGetValue(milestone, out imageUrl!);

    public static string GetMedalsCollectionFileName(ulong userId) => $"medals-{userId}.png";

    public static string GetMilestoneDescription(int tradeCount)
    {
        return tradeCount switch
        {
            1 => AppLocalization.Get(LocalizationKeys.DiscordMilestoneFirst),
            50 => AppLocalization.Get(LocalizationKeys.DiscordMilestone50),
            100 => AppLocalization.Get(LocalizationKeys.DiscordMilestone100),
            150 => AppLocalization.Get(LocalizationKeys.DiscordMilestone150),
            200 => AppLocalization.Get(LocalizationKeys.DiscordMilestone200),
            250 => AppLocalization.Get(LocalizationKeys.DiscordMilestone250),
            300 => AppLocalization.Get(LocalizationKeys.DiscordMilestone300),
            350 => AppLocalization.Get(LocalizationKeys.DiscordMilestone350),
            400 => AppLocalization.Get(LocalizationKeys.DiscordMilestone400),
            450 => AppLocalization.Get(LocalizationKeys.DiscordMilestone450),
            500 => AppLocalization.Get(LocalizationKeys.DiscordMilestone500),
            550 => AppLocalization.Get(LocalizationKeys.DiscordMilestone550),
            600 => AppLocalization.Get(LocalizationKeys.DiscordMilestone600),
            650 => AppLocalization.Get(LocalizationKeys.DiscordMilestone650),
            700 => AppLocalization.Get(LocalizationKeys.DiscordMilestone700),
            _ => AppLocalization.Format(LocalizationKeys.DiscordMilestoneDefault, tradeCount),
        };
    }

    public static MessageComponent CreateMilestoneComponent(IUser user, int milestone, int totalTrades)
    {
        var displayName = GetDisplayName(user);
        var status = GetMedalStatus(milestone);
        var unlockSummary = GetMilestoneSummary(milestone, totalTrades);
        var title = AppLocalization.Format(LocalizationKeys.DiscordMilestoneMedalTitle, displayName);
        var details = TrimComponentText(
            $"**{AppLocalization.Get(LocalizationKeys.DiscordMilestoneTitleLabel)}:** {status}\n" +
            $"**{AppLocalization.Get(LocalizationKeys.DiscordMilestoneBadgeLabel)}:** {FormatBadgeMilestone(milestone)}\n" +
            $"**{AppLocalization.Get(LocalizationKeys.DiscordMilestoneTotalTradesLabel)}:** {totalTrades}");
        var progress = TrimComponentText(GetNextBadgeProgress(totalTrades));
        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(MedalColor);
        var headerContent = new TextDisplayBuilder(TrimComponentText($"## {title}\n> **{unlockSummary}**"));

        if (milestone > 0 && TryGetMilestoneImageUrl(milestone, out var imageUrl))
        {
            container.WithSection(new SectionBuilder()
                .AddComponent(headerContent)
                .WithAccessory(new ThumbnailBuilder(
                    new UnfurledMediaItemProperties(imageUrl),
                    title,
                    false)));
        }
        else
        {
            container.WithTextDisplay(headerContent);
        }

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(new TextDisplayBuilder(details));
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(new TextDisplayBuilder(
            TrimComponentText($"## {AppLocalization.Get(LocalizationKeys.DiscordMilestoneProgressTitle)}\n{progress}")));

        builder.WithContainer(container);
        return builder.Build();
    }

    public static MessageComponent CreateMedalsShowcaseComponent(IUser user, int totalTrades)
    {
        var displayName = GetDisplayName(user);
        var avatarUrl = user.GetAvatarUrl(size: 128) ?? user.GetDefaultAvatarUrl();
        var currentMilestone = GetCurrentMilestone(totalTrades);
        var currentTitle = GetMedalStatus(currentMilestone);
        var earnedCount = MilestonesAscending.Count(milestone => totalTrades >= milestone);
        var nextMilestone = GetNextMilestone(totalTrades);
        var nextBadgeInfo = nextMilestone > 0
            ? AppLocalization.Format(LocalizationKeys.DiscordProfileNextBadgeInfo, nextMilestone - totalTrades, GetBadgeEmoji(nextMilestone), nextMilestone)
            : AppLocalization.Get(LocalizationKeys.DiscordProfileAllBadgesUnlocked);
        var nextTitle = nextMilestone > 0
            ? GetMedalStatus(nextMilestone)
            : AppLocalization.Get(LocalizationKeys.DiscordProfileMaxTitle);

        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(MedalColor);

        container.WithSection(new SectionBuilder()
            .AddComponent(new TextDisplayBuilder(TrimComponentText(
                $"## {AppLocalization.Format(LocalizationKeys.DiscordProfileBadgesTitle, displayName)}\n" +
                $"**{AppLocalization.Format(LocalizationKeys.DiscordProfileUnlocked, earnedCount, MilestonesAscending.Length)}**\n" +
                $"`{AppLocalization.Get(LocalizationKeys.DiscordProfileTrades)}` **{totalTrades:N0}**\n" +
                $"**{AppLocalization.Get(LocalizationKeys.DiscordMilestoneTitleLabel)}:** {currentTitle}")))
            .WithAccessory(new ThumbnailBuilder(
                new UnfurledMediaItemProperties(avatarUrl),
                displayName,
                false)));

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText(
            $"## 🏅 {AppLocalization.Get(LocalizationKeys.DiscordProfileBadgeCase)}\n" +
            $"{earnedCount}/{MilestonesAscending.Length} {AppLocalization.Get(LocalizationKeys.DiscordProfileBadges).ToLowerInvariant()}"));
        container.WithMediaGallery(new MediaGalleryBuilder()
            .AddItem($"attachment://{GetMedalsCollectionFileName(user.Id)}", AppLocalization.Get(LocalizationKeys.DiscordProfileBadgeCase), false));

        AddShowcaseSection(container, AppLocalization.Get(LocalizationKeys.DiscordProfileNextBadge), nextBadgeInfo);
        AddShowcaseSection(container, AppLocalization.Get(LocalizationKeys.DiscordProfileNextTitle), nextTitle);

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"{AppLocalization.Format(LocalizationKeys.DiscordProfileBadgesFooter, totalTrades)} • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f>"));

        builder.WithContainer(container);
        return builder.Build();
    }

    public static MemoryStream BuildMedalsCollectionImage(int totalTrades)
    {
        using var bitmap = new Drawing.Bitmap(AchievementCollectionWidth, AchievementCollectionHeight, DrawingImaging.PixelFormat.Format32bppArgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

            using var background = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 36, 34, 44));
            graphics.FillRectangle(background, 0, 0, AchievementCollectionWidth, AchievementCollectionHeight);

            using var glowPath = CreateEllipsePath(new Drawing.RectangleF(-140, -190, 440, 380));
            using var glow = new Drawing2D.PathGradientBrush(glowPath)
            {
                CenterColor = Drawing.Color.FromArgb(58, 255, 214, 126),
                SurroundColors = [Drawing.Color.FromArgb(0, 255, 214, 126)]
            };
            graphics.FillEllipse(glow, -140, -190, 440, 380);

            const int columns = 5;
            const int iconSize = 148;
            const int topPadding = 44;
            const int sidePadding = 42;
            const int rowGap = 26;
            var slotWidth = (AchievementCollectionWidth - sidePadding * 2) / (float)columns;
            for (var i = 0; i < MilestonesAscending.Length; i++)
            {
                var milestone = MilestonesAscending[i];
                var row = i / columns;
                var column = i % columns;
                var x = sidePadding + column * slotWidth + (slotWidth - iconSize) / 2;
                var y = topPadding + row * (iconSize + rowGap);
                DrawAchievementIcon(graphics, milestone, totalTrades >= milestone, new Drawing.RectangleF(x, y, iconSize, iconSize));
            }
        }

        var stream = new MemoryStream();
        bitmap.Save(stream, DrawingImaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    public static Embed CreateMedalsEmbed(SocketUser user, int milestone, int totalTrades)
    {
        string status = GetMedalStatus(milestone);

        string description = AppLocalization.Format(LocalizationKeys.DiscordMedalDescription, totalTrades, status);

        if (milestone > 0)
        {
            string imageUrl = MilestoneImages.GetValueOrDefault(milestone, $"https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/{milestone:D3}.png");
            return new EmbedBuilder()
                .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordTradingStatusTitle, user.Username))
                .WithColor(MedalColor)
                .WithDescription(description)
                .WithThumbnailUrl(imageUrl)
                .Build();
        }
        else
        {
            return new EmbedBuilder()
                .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordTradingStatusTitle, user.Username))
                .WithColor(MedalColor)
                .WithDescription(description)
                .Build();
        }
    }

    private static string GetMedalStatus(int milestone) =>
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

    private static int GetNextMilestone(int totalTrades) =>
        MilestonesAscending.FirstOrDefault(m => totalTrades < m, 0);

    private static string GetNextBadgeProgress(int totalTrades)
    {
        var nextMilestone = GetNextMilestone(totalTrades);
        if (nextMilestone == 0)
            return AppLocalization.Get(LocalizationKeys.DiscordMilestoneAllBadgesUnlocked);

        var remaining = nextMilestone - totalTrades;
        return AppLocalization.Format(LocalizationKeys.DiscordMilestoneNextBadgeRemaining, remaining, totalTrades, nextMilestone);
    }

    private static string FormatBadgeMilestone(int milestone) =>
        milestone <= 1 ? "1 trade" : $"{milestone} trades";

    private static void AddShowcaseSection(ContainerBuilder container, string title, string body)
    {
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"## {title}\n{body}"));
    }

    private static string GetBadgeEmoji(int milestone) =>
        SysCordSettings.Settings.CustomBadgeEmojis
            .FirstOrDefault(b => b.TradeCount == milestone)?.Emoji ?? "🏅";

    private static string GetMilestoneSummary(int milestone, int totalTrades)
    {
        if (milestone <= 0)
            return AppLocalization.Format(LocalizationKeys.DiscordMedalDescription, totalTrades, GetMedalStatus(milestone));

        if (totalTrades != milestone)
            return AppLocalization.Get(LocalizationKeys.DiscordMilestoneCurrentSummary);

        return milestone == 1
            ? AppLocalization.Get(LocalizationKeys.DiscordMilestoneFirstTradeUnlock)
            : AppLocalization.Format(LocalizationKeys.DiscordMilestoneTradeUnlock, milestone);
    }

    private static string GetDisplayName(IUser user)
    {
        if (user is IGuildUser guildUser && !string.IsNullOrWhiteSpace(guildUser.Nickname))
            return guildUser.Nickname;

        if (!string.IsNullOrWhiteSpace(user.GlobalName))
            return user.GlobalName;

        return user.Username;
    }

    private static string TrimComponentText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "\u200B";

        return text.Length <= MaxComponentTextLength ? text : text[..(MaxComponentTextLength - 3)] + "...";
    }

    private static void DrawAchievementIcon(Drawing.Graphics graphics, int milestone, bool unlocked, Drawing.RectangleF bounds)
    {
        var assetPath = unlocked
            ? GetAchievementMedalAssetPath(milestone)
            : GetAchievementSilhouetteAssetPath(milestone);

        using var shadow = new Drawing.SolidBrush(Drawing.Color.FromArgb(unlocked ? 72 : 42, 0, 0, 0));
        graphics.FillEllipse(shadow, bounds.X + bounds.Width * 0.12f, bounds.Bottom - 18, bounds.Width * 0.76f, 18);

        if (!string.IsNullOrWhiteSpace(assetPath) && File.Exists(assetPath))
        {
            try
            {
                using var image = Drawing.Image.FromFile(assetPath);
                DrawImageContained(graphics, image, bounds);
                return;
            }
            catch
            {
                // Fall through to a drawn locked slot if an asset is missing or malformed.
            }
        }

        DrawAchievementFallback(graphics, bounds, unlocked);
    }

    private static void DrawAchievementFallback(Drawing.Graphics graphics, Drawing.RectangleF bounds, bool unlocked)
    {
        using var fill = new Drawing2D.LinearGradientBrush(
            bounds,
            unlocked ? Drawing.Color.FromArgb(255, 248, 202, 70) : Drawing.Color.FromArgb(255, 188, 190, 205),
            unlocked ? Drawing.Color.FromArgb(255, 230, 119, 44) : Drawing.Color.FromArgb(255, 125, 128, 146),
            90f);
        graphics.FillEllipse(fill, bounds);

        using var pen = new Drawing.Pen(Drawing.Color.FromArgb(190, 255, 255, 255), Math.Max(4, bounds.Width / 26));
        graphics.DrawLine(pen, bounds.X + bounds.Width * 0.32f, bounds.Y + bounds.Height * 0.5f, bounds.X + bounds.Width * 0.68f, bounds.Y + bounds.Height * 0.5f);
        graphics.DrawLine(pen, bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.32f, bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.68f);
    }

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

    private static void DrawImageContained(Drawing.Graphics graphics, Drawing.Image image, Drawing.RectangleF bounds)
    {
        var scale = Math.Min(bounds.Width / image.Width, bounds.Height / image.Height);
        var width = image.Width * scale;
        var height = image.Height * scale;
        var x = bounds.X + (bounds.Width - width) / 2;
        var y = bounds.Y + (bounds.Height - height) / 2;
        graphics.DrawImage(image, x, y, width, height);
    }

    private static Drawing2D.GraphicsPath CreateEllipsePath(Drawing.RectangleF bounds)
    {
        var path = new Drawing2D.GraphicsPath();
        path.AddEllipse(bounds);
        return path;
    }
}
