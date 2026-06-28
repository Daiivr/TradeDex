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
using Drawing2D = System.Drawing.Drawing2D;
using DrawingImaging = System.Drawing.Imaging;
using DrawingText = System.Drawing.Text;

#pragma warning disable CA1416

namespace SysBot.Pokemon.Discord;

public class ProfileModule : ModuleBase<SocketCommandContext>
{
    private const string StatsFilePath = "user_stats.json";
    private const int MaxComponentTextLength = 3900;
    private const int MaxBioLength = 280;
    private const string BioButtonCustomId = "profile_edit_bio";
    private const string BioInputCustomId = "profile_bio_input";
    private const string BioModalPrefix = "profile_bio_modal:";
    private const int LevelCardWidth = 1000;
    private const int LevelCardHeight = 320;
    private const int AchievementPreviewWidth = 1000;
    private const int AchievementPreviewHeight = 240;
    private const int AchievementCollectionWidth = 1000;
    private const int AchievementCollectionHeight = 620;
    private static readonly int[] AchievementMilestones = [1, 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700];
    private static readonly HttpClient Http = new();

    [Command("profile")]
    [Alias("tp", "perfil")]
    [Summary("Muestra la informacion del perfil de un usuario, con detalles sensibles visibles solo para el propietario del perfil.")]
    public async Task ProfileAsync(IUser? user = null)
    {
        var targetUser = user ?? Context.User;
        var isSelfProfile = targetUser.Id == Context.User.Id;

        if (isSelfProfile)
        {
            try
            {
                var dmChannel = await targetUser.CreateDMChannelAsync().ConfigureAwait(false);
                var (message, hasLevelCard) = await SendProfileMessageAsync(dmChannel, targetUser, isSelfProfile).ConfigureAwait(false);
                if (Context.Guild != null)
                {
                    var confirmation = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordProfileSentDm, targetUser.Mention)).ConfigureAwait(false);
                    _ = DeleteAfterDelayAsync(confirmation, TimeSpan.FromSeconds(10));
                }

                _ = DeleteAfterDelayAsync(Context.Message, TimeSpan.Zero);
                _ = HandleProfileInteractionsAsync(message, Context.User.Id, TimeSpan.FromMinutes(1), targetUser.Id, hasLevelCard);
            }
            catch
            {
                var error = await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordProfileDmFailed, targetUser.Mention)).ConfigureAwait(false);
                _ = DeleteAfterDelayAsync(error, TimeSpan.FromSeconds(10));
                _ = DeleteAfterDelayAsync(Context.Message, TimeSpan.Zero);
            }

            return;
        }

        var (publicMessage, publicHasLevelCard) = await SendProfileMessageAsync(Context.Channel, targetUser, isSelfProfile).ConfigureAwait(false);
        _ = HandleProfileInteractionsAsync(publicMessage, Context.User.Id, TimeSpan.FromMinutes(1), targetUser.Id, publicHasLevelCard);
    }

    private async Task<(IUserMessage Message, bool HasLevelCard)> SendProfileMessageAsync(IMessageChannel channel, IUser targetUser, bool includePrivateInfo)
    {
        var fileName = GetLevelCardFileName(targetUser.Id);
        try
        {
            using var cardStream = await BuildLevelCardImageAsync(targetUser).ConfigureAwait(false);
            using var achievementPreviewStream = BuildAchievementPreviewImage(targetUser.Id);
            using var achievementCollectionStream = BuildAchievementCollectionImage(targetUser.Id);
            var component = await BuildProfileComponentAsync(targetUser, includePrivateInfo, includeLevelCard: true).ConfigureAwait(false);
            var attachments = new[]
            {
                new FileAttachment(cardStream, fileName, AppLocalization.Get(LocalizationKeys.DiscordProfileProgress)),
                new FileAttachment(achievementPreviewStream, GetAchievementPreviewFileName(targetUser.Id), AppLocalization.Get(LocalizationKeys.DiscordProfileLatestAchievements)),
                new FileAttachment(achievementCollectionStream, GetAchievementCollectionFileName(targetUser.Id), AppLocalization.Get(LocalizationKeys.DiscordProfileBadgeCase)),
            };
            var message = await channel.SendFilesAsync(attachments, components: component, flags: MessageFlags.ComponentsV2).ConfigureAwait(false);
            return (message, true);
        }
        catch
        {
            var fallback = await BuildProfileComponentAsync(targetUser, includePrivateInfo, includeLevelCard: false).ConfigureAwait(false);
            var message = await channel.SendMessageAsync(components: fallback, flags: MessageFlags.ComponentsV2).ConfigureAwait(false);
            return (message, false);
        }
    }

    private async Task<MessageComponent> BuildProfileComponentAsync(IUser targetUser, bool includePrivateInfo, bool disableActions = false, bool includeLevelCard = false)
    {
        var avatarUrl = targetUser.GetAvatarUrl(size: 128) ?? targetUser.GetDefaultAvatarUrl();
        var displayName = GetDisplayName(targetUser);
        var tradeCount = GetTradeCountForUser(targetUser.Id);
        var (xp, level) = GetGameStatsForUser(targetUser.Id.ToString());
        var currentStatus = GetCurrentStatus(tradeCount);
        var requiredXp = GetRequiredXPForNextLevel(level);
        var xpProgress = requiredXp <= 0 ? 0 : Math.Clamp((double)xp / requiredXp * 100, 0, 100);
        var accountCreated = $"<t:{targetUser.CreatedAt.ToUnixTimeSeconds()}:R>";
        var tradeDetails = new TradeCodeStorage().GetTradeDetails(targetUser.Id);
        var accountCreatedDate = $"<t:{targetUser.CreatedAt.ToUnixTimeSeconds()}:D>";
        var league = GetLeagueForLevel(level);
        var rank = GetCommunityRankForUser(targetUser.Id, level, xp, league, Context.Client.Guilds);
        var rankText = $"#{rank:N0}";
        var earnedBadgeCount = GetEarnedBadgeCount(tradeCount);
        var totalBadgeCount = SysCordSettings.Settings.CustomBadgeEmojis.Count;
        var userStats = string.Join("\n",
            FormatStatLine(AppLocalization.Get(LocalizationKeys.DiscordProfileCurrentTitle), currentStatus),
            FormatStatLine(AppLocalization.Get(LocalizationKeys.DiscordProfileTrades), tradeCount.ToString("N0")),
            FormatStatLine(AppLocalization.Get(LocalizationKeys.DiscordProfileBadges), $"{earnedBadgeCount}/{totalBadgeCount}"));
        var progress = string.Join("\n",
            $"**{AppLocalization.Get(LocalizationKeys.DiscordProfileLevel)} {level}**",
            GetProgressBar(xpProgress),
            $"`XP` **{xp:N0}/{requiredXp:N0}** • {AppLocalization.Format(LocalizationKeys.DiscordProfileXpToNextLevel, Math.Max(0, requiredXp - xp).ToString("N0"))}");
        var activity = string.Join("\n",
            FormatStatLine(AppLocalization.Get(LocalizationKeys.DiscordProfileLastTrade), GetLastTradeText(tradeDetails)),
            FormatStatLine(AppLocalization.Get(LocalizationKeys.DiscordProfileCreated), accountCreated));
        var achievements = GetAchievementPreviewText(tradeCount);
        var color = await GetDominantColorAsync(avatarUrl).ConfigureAwait(false);
        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(color);

        var header = new SectionBuilder()
            .AddComponent(new TextDisplayBuilder(TrimComponentText(
                $"# {displayName}\n" +
                $"{currentStatus}\n" +
                AppLocalization.Format(LocalizationKeys.DiscordProfileCommunitySince, accountCreatedDate))))
            .WithAccessory(new ThumbnailBuilder(
                new UnfurledMediaItemProperties(avatarUrl),
                displayName,
                false));

        container.WithSection(header);
        AddAboutSection(container, GetAboutText(tradeDetails), includePrivateInfo && targetUser.Id == Context.User.Id, disableActions);
        AddProfileSection(container, $"📌 {AppLocalization.Get(LocalizationKeys.DiscordProfileUserStats)}", userStats);
        if (includeLevelCard)
            AddLevelCardSection(container, targetUser.Id, league, rankText);
        else
            AddFeaturedProfileSection(container, $"{league.Emoji} {league.DisplayName}", AppLocalization.Format(LocalizationKeys.DiscordProfileLeagueRankValue, rankText), progress);

        if (includeLevelCard)
            AddAchievementPreviewSection(container, targetUser.Id, tradeCount, disableActions);
        else
            AddProfileSection(container, $"🎖️ {AppLocalization.Get(LocalizationKeys.DiscordProfileLatestAchievements)}", achievements);
        AddProfileSection(container, $"📊 {AppLocalization.Get(LocalizationKeys.DiscordProfileEngagementStats)}", activity);

        if (includePrivateInfo)
        {
            var (ot, sid, tid) = GetTrainerInfo(targetUser.Id);
            var privateInfo = string.Join("\n",
                FormatStatLine("OT", ot),
                FormatStatLine("SID", sid),
                FormatStatLine("TID", tid),
                FormatStatLine(AppLocalization.Get(LocalizationKeys.DiscordProfileTradeCode), GetTradeCodeForUser(targetUser.Id) ?? AppLocalization.Get(LocalizationKeys.DiscordProfileNoTradeCode)));
            AddProfileSection(container, AppLocalization.Get(LocalizationKeys.DiscordProfilePrivateInfo), privateInfo);
        }

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"{GetProfileFooter(includePrivateInfo)} • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f>"));

        builder.WithContainer(container);
        return builder.Build();
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

    private async Task<MemoryStream> BuildLevelCardImageAsync(IUser targetUser)
    {
        var avatarUrl = targetUser.GetAvatarUrl(size: 256) ?? targetUser.GetDefaultAvatarUrl();
        var displayName = GetDisplayName(targetUser);
        var tradeCount = GetTradeCountForUser(targetUser.Id);
        var currentStatus = GetCurrentStatus(tradeCount);
        var (xp, level) = GetGameStatsForUser(targetUser.Id.ToString());
        var requiredXp = GetRequiredXPForNextLevel(level);
        var xpProgress = requiredXp <= 0 ? 0 : Math.Clamp((double)xp / requiredXp, 0, 1);
        var league = GetLeagueForLevel(level);
        var rank = GetCommunityRankForUser(targetUser.Id, level, xp, league, Context.Client.Guilds);

        using var bitmap = new Drawing.Bitmap(LevelCardWidth, LevelCardHeight, DrawingImaging.PixelFormat.Format32bppArgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

            DrawLevelCardBackground(graphics, league);
            await DrawLevelCardTrophyAsync(graphics, league).ConfigureAwait(false);
            await DrawLevelCardAvatarAsync(graphics, avatarUrl, displayName).ConfigureAwait(false);
            DrawLevelCardText(graphics, displayName, currentStatus, league, rank, level, xp, requiredXp, xpProgress);
        }

        var stream = new MemoryStream();
        bitmap.Save(stream, DrawingImaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream BuildAchievementPreviewImage(ulong targetUserId)
    {
        var tradeCount = GetTradeCountForUser(targetUserId);
        var milestones = GetAchievementPreviewMilestones(tradeCount);
        return BuildAchievementGridImage(
            milestones,
            tradeCount,
            AchievementPreviewWidth,
            AchievementPreviewHeight,
            columns: 4,
            iconSize: 158,
            topPadding: 36,
            sidePadding: 54,
            rowGap: 0);
    }

    private static MemoryStream BuildAchievementCollectionImage(ulong targetUserId)
    {
        var tradeCount = GetTradeCountForUser(targetUserId);
        return BuildAchievementGridImage(
            AchievementMilestones,
            tradeCount,
            AchievementCollectionWidth,
            AchievementCollectionHeight,
            columns: 5,
            iconSize: 148,
            topPadding: 44,
            sidePadding: 42,
            rowGap: 26);
    }

    private static MemoryStream BuildAchievementGridImage(IReadOnlyList<int> milestones, int tradeCount, int width, int height, int columns, int iconSize, int topPadding, int sidePadding, int rowGap)
    {
        using var bitmap = new Drawing.Bitmap(width, height, DrawingImaging.PixelFormat.Format32bppArgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

            using var background = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 36, 34, 44));
            graphics.FillRectangle(background, 0, 0, width, height);

            using var glowPath = CreateEllipsePath(new Drawing.RectangleF(-140, -190, 440, 380));
            using var glow = new Drawing2D.PathGradientBrush(glowPath)
            {
                CenterColor = Drawing.Color.FromArgb(58, 255, 214, 126),
                SurroundColors = new[] { Drawing.Color.FromArgb(0, 255, 214, 126) }
            };
            graphics.FillEllipse(glow, -140, -190, 440, 380);

            var slotWidth = (width - sidePadding * 2) / (float)columns;
            for (var i = 0; i < milestones.Count; i++)
            {
                var milestone = milestones[i];
                var row = i / columns;
                var column = i % columns;
                var x = sidePadding + column * slotWidth + (slotWidth - iconSize) / 2;
                var y = topPadding + row * (iconSize + rowGap);
                DrawAchievementIcon(graphics, milestone, tradeCount >= milestone, new Drawing.RectangleF(x, y, iconSize, iconSize));
            }
        }

        var stream = new MemoryStream();
        bitmap.Save(stream, DrawingImaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
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

    private static void DrawLevelCardBackground(Drawing.Graphics graphics, ProfileLeague league)
    {
        var bounds = new Drawing.Rectangle(0, 0, LevelCardWidth, LevelCardHeight);
        var leftColor = MixColors(league.Secondary, Drawing.Color.FromArgb(105, 47, 22), 0.62f);
        var rightColor = MixColors(LightenColor(league.Primary, 0.32f), Drawing.Color.FromArgb(255, 190, 92), 0.28f);
        var glowColor = LightenColor(league.Primary, 0.48f);

        using var gradient = new Drawing2D.LinearGradientBrush(
            bounds,
            leftColor,
            rightColor,
            0f);
        graphics.FillRectangle(gradient, bounds);

        var origin = new Drawing.PointF(120, 170);
        for (var i = 0; i < 14; i++)
        {
            var angle1 = Math.PI * 2 * i / 14;
            var angle2 = Math.PI * 2 * (i + 0.52) / 14;
            var p1 = new Drawing.PointF(origin.X + (float)Math.Cos(angle1) * 980, origin.Y + (float)Math.Sin(angle1) * 980);
            var p2 = new Drawing.PointF(origin.X + (float)Math.Cos(angle2) * 980, origin.Y + (float)Math.Sin(angle2) * 980);
            using var rayBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(i % 2 == 0 ? 14 : 6, 255, 255, 255));
            graphics.FillPolygon(rayBrush, new[] { origin, p1, p2 });
        }

        using var rayBlend = new Drawing2D.LinearGradientBrush(
            bounds,
            Drawing.Color.FromArgb(24, league.Primary),
            Drawing.Color.FromArgb(0, league.Primary),
            0f);
        graphics.FillRectangle(rayBlend, bounds);

        using var glowPath = CreateEllipsePath(new Drawing.RectangleF(620, -95, 430, 430));
        using var glow = new Drawing2D.PathGradientBrush(glowPath)
        {
            CenterColor = Drawing.Color.FromArgb(130, glowColor),
            SurroundColors = new[] { Drawing.Color.FromArgb(0, glowColor) }
        };
        graphics.FillEllipse(glow, 620, -95, 430, 430);

        using var vignette = new Drawing2D.LinearGradientBrush(
            bounds,
            Drawing.Color.FromArgb(95, 84, 38, 18),
            Drawing.Color.FromArgb(0, 84, 38, 18),
            0f);
        graphics.FillRectangle(vignette, bounds);

        DrawSpark(graphics, 660, 54, 23);
        DrawSpark(graphics, 938, 88, 27);
        DrawSpark(graphics, 680, 215, 24);
        DrawSpark(graphics, 916, 235, 22);
    }

    private static async Task DrawLevelCardTrophyAsync(Drawing.Graphics graphics, ProfileLeague league)
    {
        var bounds = GetLevelCardTrophyBounds(league);

        if (TryGetLeagueAssetPath(league.Key, out var assetPath))
        {
            try
            {
                using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(54, 90, 36, 12));
                graphics.FillEllipse(shadowBrush, bounds.X + 14, 270, bounds.Width - 30, 25);

                using var leagueImage = Drawing.Image.FromFile(assetPath);
                DrawTrophyGlow(graphics, league, bounds);
                DrawImageGlow(graphics, leagueImage, bounds, LightenColor(league.Primary, 0.35f));
                DrawImageContained(graphics, leagueImage, bounds);
                return;
            }
            catch
            {
                // Fall through to configured emoji or vector art if the bundled asset cannot be loaded.
            }
        }

        if (TryGetDiscordEmojiImageUrl(league.Emoji, out var emojiUrl))
        {
            try
            {
                using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(54, 90, 36, 12));
                graphics.FillEllipse(shadowBrush, bounds.X + 14, 270, bounds.Width - 30, 25);

                await using var stream = await Http.GetStreamAsync(emojiUrl).ConfigureAwait(false);
                using var emojiImage = Drawing.Image.FromStream(stream);
                DrawTrophyGlow(graphics, league, bounds);
                DrawImageGlow(graphics, emojiImage, bounds, LightenColor(league.Primary, 0.35f));
                DrawImageContained(graphics, emojiImage, bounds);
                return;
            }
            catch
            {
                // Fall back to the vector trophy if the configured Discord emoji cannot be loaded.
            }
        }

        DrawLevelCardTrophy(graphics, league);
    }

    private static Drawing.RectangleF GetLevelCardTrophyBounds(ProfileLeague league) =>
        league.IsCup
            ? new Drawing.RectangleF(682, 18, 232, 270)
            : new Drawing.RectangleF(704, 18, 190, 270);

    private static void DrawTrophyGlow(Drawing.Graphics graphics, ProfileLeague league, Drawing.RectangleF bounds)
    {
        var glowBounds = Drawing.RectangleF.Inflate(bounds, 46, 34);
        var glowColor = LightenColor(league.Primary, 0.45f);
        using var glowPath = CreateEllipsePath(glowBounds);
        using var glow = new Drawing2D.PathGradientBrush(glowPath)
        {
            CenterColor = Drawing.Color.FromArgb(118, glowColor),
            SurroundColors = new[] { Drawing.Color.FromArgb(0, glowColor) }
        };
        graphics.FillEllipse(glow, glowBounds);
    }

    private static void DrawLevelCardTrophy(Drawing.Graphics graphics, ProfileLeague league)
    {
        using var trophyShadow = new Drawing.SolidBrush(Drawing.Color.FromArgb(50, 90, 36, 12));
        graphics.FillEllipse(trophyShadow, 715, 268, 170, 24);

        if (league.IsCup)
        {
            DrawLevelCardCup(graphics, league);
            return;
        }

        using var shieldPath = new Drawing2D.GraphicsPath();
        shieldPath.AddBezier(748, 50, 785, 28, 846, 28, 880, 50);
        shieldPath.AddLine(880, 50, 866, 140);
        shieldPath.AddBezier(866, 140, 845, 190, 812, 207, 779, 190);
        shieldPath.AddBezier(779, 190, 746, 171, 724, 140, 724, 103);
        shieldPath.AddLine(724, 103, 748, 50);

        using var shieldBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.Rectangle(720, 35, 170, 180),
            Drawing.Color.FromArgb(255, 255, 236, 202),
            league.Primary,
            70f);
        graphics.FillPath(shieldBrush, shieldPath);

        using var shieldPen = new Drawing.Pen(Drawing.Color.FromArgb(210, 255, 248, 226), 10)
        {
            LineJoin = Drawing2D.LineJoin.Round
        };
        graphics.DrawPath(shieldPen, shieldPath);

        using var innerPen = new Drawing.Pen(Drawing.Color.FromArgb(145, league.Secondary), 6)
        {
            LineJoin = Drawing2D.LineJoin.Round
        };
        graphics.DrawPath(innerPen, shieldPath);

        using var stemBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.Rectangle(788, 203, 48, 62),
            Drawing.Color.FromArgb(236, 246, 250),
            Drawing.Color.FromArgb(118, 142, 151),
            90f);
        FillRoundedRect(graphics, stemBrush, new Drawing.RectangleF(790, 203, 45, 62), 14);

        using var baseBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.Rectangle(745, 250, 140, 40),
            Drawing.Color.FromArgb(240, 248, 251),
            Drawing.Color.FromArgb(99, 121, 132),
            90f);
        FillRoundedRect(graphics, baseBrush, new Drawing.RectangleF(748, 246, 132, 42), 12);
        FillRoundedRect(graphics, baseBrush, new Drawing.RectangleF(766, 226, 96, 34), 12);

        using var plateBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(170, 227, 236, 240));
        FillRoundedRect(graphics, plateBrush, new Drawing.RectangleF(786, 258, 55, 14), 4);
    }

    private static void DrawLevelCardCup(Drawing.Graphics graphics, ProfileLeague league)
    {
        using var cupPath = new Drawing2D.GraphicsPath();
        cupPath.AddBezier(742, 52, 770, 32, 852, 32, 882, 52);
        cupPath.AddBezier(882, 52, 872, 156, 824, 198, 812, 198);
        cupPath.AddBezier(800, 198, 752, 156, 742, 52, 742, 52);

        using var cupBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.Rectangle(730, 35, 168, 176),
            Drawing.Color.FromArgb(255, 255, 245, 230),
            league.Primary,
            70f);
        graphics.FillPath(cupBrush, cupPath);

        using var handlePen = new Drawing.Pen(Drawing.Color.FromArgb(175, league.Secondary), 13)
        {
            StartCap = Drawing2D.LineCap.Round,
            EndCap = Drawing2D.LineCap.Round,
            LineJoin = Drawing2D.LineJoin.Round
        };
        graphics.DrawBezier(handlePen, 746, 84, 704, 78, 700, 148, 751, 145);
        graphics.DrawBezier(handlePen, 878, 84, 920, 78, 924, 148, 873, 145);

        using var rimPen = new Drawing.Pen(Drawing.Color.FromArgb(230, 255, 248, 226), 9)
        {
            LineJoin = Drawing2D.LineJoin.Round
        };
        graphics.DrawPath(rimPen, cupPath);

        using var innerPen = new Drawing.Pen(Drawing.Color.FromArgb(150, league.Secondary), 5)
        {
            LineJoin = Drawing2D.LineJoin.Round
        };
        graphics.DrawPath(innerPen, cupPath);

        using var stemBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.Rectangle(788, 197, 48, 66),
            Drawing.Color.FromArgb(236, 246, 250),
            Drawing.Color.FromArgb(118, 142, 151),
            90f);
        FillRoundedRect(graphics, stemBrush, new Drawing.RectangleF(790, 197, 45, 68), 14);

        using var baseBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.Rectangle(745, 247, 140, 42),
            Drawing.Color.FromArgb(240, 248, 251),
            Drawing.Color.FromArgb(99, 121, 132),
            90f);
        FillRoundedRect(graphics, baseBrush, new Drawing.RectangleF(748, 246, 132, 42), 12);
        FillRoundedRect(graphics, baseBrush, new Drawing.RectangleF(766, 226, 96, 34), 12);
    }

    private static async Task DrawLevelCardAvatarAsync(Drawing.Graphics graphics, string avatarUrl, string displayName)
    {
        var bounds = new Drawing.RectangleF(48, 45, 150, 150);
        using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(75, 65, 30, 15));
        graphics.FillEllipse(shadowBrush, bounds.X + 5, bounds.Y + 9, bounds.Width, bounds.Height);

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
            using var fallback = new Drawing2D.LinearGradientBrush(bounds, Drawing.Color.FromArgb(255, 96, 48, 39), Drawing.Color.FromArgb(255, 244, 205, 82), 45f);
            graphics.FillEllipse(fallback, bounds);

            using var initialFont = new Drawing.Font("Segoe UI", 58, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
            using var initialBrush = new Drawing.SolidBrush(Drawing.Color.White);
            var initial = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Trim()[0].ToString().ToUpperInvariant();
            DrawCenteredText(graphics, initial, initialFont, initialBrush, bounds);
        }
        finally
        {
            graphics.Clip = previousClip;
        }

        using var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(255, 121, 62, 22), 7);
        graphics.DrawEllipse(borderPen, bounds);
        using var innerPen = new Drawing.Pen(Drawing.Color.FromArgb(220, 255, 244, 210), 3);
        graphics.DrawEllipse(innerPen, Drawing.RectangleF.Inflate(bounds, -5, -5));
    }

    private static void DrawLevelCardText(Drawing.Graphics graphics, string displayName, string currentStatus, ProfileLeague league, int rank, int level, int xp, int requiredXp, double progress)
    {
        using var rankFont = new Drawing.Font("Segoe UI", 74, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var nameFont = new Drawing.Font("Segoe UI", 38, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var metaFont = new Drawing.Font("Segoe UI", 20, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var titleFont = new Drawing.Font("Segoe UI", 18, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var xpFont = new Drawing.Font("Segoe UI", 24, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
        using var whiteBrush = new Drawing.SolidBrush(Drawing.Color.White);
        using var softBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(230, 255, 245, 224));
        using var titleBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(215, 255, 238, 212));

        DrawTextWithShadow(graphics, $"#{rank:N0}", rankFont, whiteBrush, 235, 74);
        DrawTextWithShadow(graphics, TrimForCard(displayName, 20), nameFont, whiteBrush, 50, 210);
        DrawTextWithShadow(graphics, $"{AppLocalization.Get(LocalizationKeys.DiscordProfileLevel)} {level} • {TrimForCard(league.DisplayName, 24)}", metaFont, softBrush, 235, 158);
        DrawTextWithShadow(graphics, TrimForCard(currentStatus, 34), titleFont, titleBrush, 235, 184);

        var barBounds = new Drawing.RectangleF(50, 264, 365, 30);
        using var barBack = new Drawing.SolidBrush(Drawing.Color.FromArgb(235, 28, 28, 30));
        FillRoundedRect(graphics, barBack, barBounds, 15);

        var fillWidth = Math.Max(12, (float)(barBounds.Width * progress));
        using var fillBrush = new Drawing2D.LinearGradientBrush(
            new Drawing.RectangleF(barBounds.X, barBounds.Y, Math.Max(1, fillWidth), barBounds.Height),
            Drawing.Color.FromArgb(255, 255, 249, 235),
            Drawing.Color.FromArgb(255, 255, 128, 54),
            0f);
        FillRoundedRect(graphics, fillBrush, new Drawing.RectangleF(barBounds.X, barBounds.Y, fillWidth, barBounds.Height), 15);

        using var barPen = new Drawing.Pen(Drawing.Color.FromArgb(245, 255, 255, 255), 3);
        DrawRoundedRect(graphics, barPen, barBounds, 15);
        DrawTextWithShadow(graphics, $"{xp:N0}/{requiredXp:N0} XP", xpFont, whiteBrush, 428, 261);
    }

    private static void DrawSpark(Drawing.Graphics graphics, float x, float y, float size)
    {
        using var pen = new Drawing.Pen(Drawing.Color.FromArgb(235, 255, 255, 255), 8)
        {
            StartCap = Drawing2D.LineCap.Round,
            EndCap = Drawing2D.LineCap.Round
        };
        graphics.DrawLine(pen, x - size / 2, y, x + size / 2, y);
        graphics.DrawLine(pen, x, y - size / 2, x, y + size / 2);
    }

    private static void DrawTextWithShadow(Drawing.Graphics graphics, string text, Drawing.Font font, Drawing.Brush brush, float x, float y)
    {
        using var shadowBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(70, 83, 38, 16));
        graphics.DrawString(text, font, shadowBrush, x + 3, y + 4);
        graphics.DrawString(text, font, brush, x, y);
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
        DrawTintedImage(graphics, image, GetContainedImageBounds(image, Drawing.RectangleF.Inflate(bounds, 34, 26)), glowColor, 0.11f);
        DrawTintedImage(graphics, image, GetContainedImageBounds(image, Drawing.RectangleF.Inflate(bounds, 18, 14)), glowColor, 0.22f);
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
        graphics.DrawImage(
            image,
            destination,
            0,
            0,
            image.Width,
            image.Height,
            Drawing.GraphicsUnit.Pixel,
            attributes);
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

    private static bool TryGetDiscordEmojiImageUrl(string emoji, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(emoji))
            return false;

        var value = emoji.Trim();
        if (value.StartsWith('<') && value.EndsWith('>'))
        {
            var lastColon = value.LastIndexOf(':');
            if (lastColon < 0 || lastColon + 1 >= value.Length - 1)
                return false;

            value = value[(lastColon + 1)..^1];
        }

        if (!ulong.TryParse(value, out var emojiId))
            return false;

        url = $"https://cdn.discordapp.com/emojis/{emojiId}.png?size=256&quality=lossless";
        return true;
    }

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

    private static Drawing2D.GraphicsPath CreateEllipsePath(Drawing.RectangleF bounds)
    {
        var path = new Drawing2D.GraphicsPath();
        path.AddEllipse(bounds);
        return path;
    }

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

    private static string TrimForCard(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
    }

    private async Task HandleProfileInteractionsAsync(IUserMessage message, ulong userId, TimeSpan timeout, ulong targetUserId, bool hasLevelCard)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        var activeView = "profile";

        while (!timeoutCts.IsCancellationRequested)
        {
            var interaction = await WaitForProfileInteractionAsync(message, userId, targetUserId, timeoutCts.Token).ConfigureAwait(false);
            if (interaction == null)
                break;

            timeoutCts.CancelAfter(timeout);

            if (interaction is SocketMessageComponent component)
            {
                var selectedOption = component.Data.CustomId;
                if (selectedOption == BioButtonCustomId)
                {
                    await component.RespondWithModalAsync(BuildBioModal(targetUserId)).ConfigureAwait(false);
                    continue;
                }

                await component.DeferAsync().ConfigureAwait(false);
                if (selectedOption == "profile_view_badges")
                {
                    activeView = "badges";
                    await ShowBadgesAsync(message, targetUserId).ConfigureAwait(false);
                }
                else if (selectedOption == "profile_back_to_profile")
                {
                    activeView = "profile";
                    var targetUser = GetUser(targetUserId) ?? Context.User;
                    var newComponent = await BuildProfileComponentAsync(targetUser, targetUser.Id == Context.User.Id, includeLevelCard: hasLevelCard).ConfigureAwait(false);
                    await message.ModifyAsync(msg =>
                    {
                        msg.Components = newComponent;
                    }).ConfigureAwait(false);
                }
            }
            else if (interaction is SocketModal modal)
            {
                activeView = "profile";
                var bio = modal.Data.Components.FirstOrDefault(c => c.CustomId == BioInputCustomId)?.Value;
                new TradeCodeStorage().UpdateQuote(targetUserId, bio);

                var targetUser = GetUser(targetUserId) ?? Context.User;
                var newComponent = await BuildProfileComponentAsync(targetUser, targetUser.Id == Context.User.Id, includeLevelCard: hasLevelCard).ConfigureAwait(false);
                await modal.RespondAsync(AppLocalization.Get(LocalizationKeys.DiscordProfileBioUpdated), ephemeral: modal.GuildId.HasValue).ConfigureAwait(false);
                await message.ModifyAsync(msg =>
                {
                    msg.Components = newComponent;
                }).ConfigureAwait(false);
            }
        }

        var finalUser = GetUser(targetUserId) ?? Context.User;
        var finalComponent = activeView == "badges"
            ? await BuildBadgesComponentAsync(finalUser, true).ConfigureAwait(false)
            : await BuildProfileComponentAsync(finalUser, finalUser.Id == Context.User.Id, true, hasLevelCard).ConfigureAwait(false);
        await message.ModifyAsync(msg => msg.Components = finalComponent).ConfigureAwait(false);
    }

    private async Task ShowBadgesAsync(IUserMessage message, ulong targetUserId)
    {
        var targetUser = GetUser(targetUserId) ?? Context.User;
        var component = await BuildBadgesComponentAsync(targetUser).ConfigureAwait(false);

        await message.ModifyAsync(msg =>
        {
            msg.Components = component;
        }).ConfigureAwait(false);
    }

    private async Task<MessageComponent> BuildBadgesComponentAsync(IUser targetUser, bool disableActions = false)
    {
        var tradeCount = GetTradeCountForUser(targetUser.Id);
        var avatarUrl = targetUser.GetAvatarUrl(size: 128) ?? targetUser.GetDefaultAvatarUrl();
        var displayName = GetDisplayName(targetUser);
        var badgeList = SysCordSettings.Settings.CustomBadgeEmojis.OrderBy(b => b.TradeCount).ToList();
        var nextBadge = badgeList.FirstOrDefault(b => b.TradeCount > tradeCount);
        var nextBadgeInfo = nextBadge != null
            ? AppLocalization.Format(LocalizationKeys.DiscordProfileNextBadgeInfo, nextBadge.TradeCount - tradeCount, nextBadge.Emoji, nextBadge.TradeCount)
            : AppLocalization.Get(LocalizationKeys.DiscordProfileAllBadgesUnlocked);
        var nextTitle = nextBadge != null ? GetCurrentStatus(nextBadge.TradeCount) : AppLocalization.Get(LocalizationKeys.DiscordProfileMaxTitle);
        var earnedBadgeCount = GetEarnedBadgeCount(tradeCount);
        var totalBadgeCount = badgeList.Count;
        var color = await GetDominantColorAsync(avatarUrl).ConfigureAwait(false);
        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(color);

        var header = new SectionBuilder()
            .AddComponent(new TextDisplayBuilder(TrimComponentText(
                $"## {AppLocalization.Format(LocalizationKeys.DiscordProfileBadgesTitle, displayName)}\n" +
                $"**{AppLocalization.Format(LocalizationKeys.DiscordProfileUnlocked, earnedBadgeCount, totalBadgeCount)}**\n" +
                $"`{AppLocalization.Get(LocalizationKeys.DiscordProfileTrades)}` **{tradeCount:N0}**")))
            .WithAccessory(new ThumbnailBuilder(
                new UnfurledMediaItemProperties(avatarUrl),
                displayName,
                false));

        container.WithSection(header);
        AddAchievementCollectionSection(container, targetUser.Id, tradeCount, disableActions);

        AddProfileSection(container, AppLocalization.Get(LocalizationKeys.DiscordProfileNextBadge), nextBadgeInfo);
        AddProfileSection(container, AppLocalization.Get(LocalizationKeys.DiscordProfileNextTitle), nextTitle);
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"{AppLocalization.Format(LocalizationKeys.DiscordProfileBadgesFooter, tradeCount)} • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f>"));

        builder.WithContainer(container);
        return builder.Build();
    }

    private string GetProfileFooter(bool includePrivateInfo)
    {
        var serverName = Context.Guild?.Name ?? AppLocalization.Get(LocalizationKeys.DiscordProfileThisServer);
        return includePrivateInfo
            ? AppLocalization.Format(LocalizationKeys.DiscordProfileServerFooter, serverName)
            : AppLocalization.Format(LocalizationKeys.DiscordProfilePublicFooter, serverName);
    }

    private static void AddProfileSection(ContainerBuilder container, string title, string body)
    {
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"## {title}\n{body}"));
    }

    private static void AddFeaturedProfileSection(ContainerBuilder container, string title, string subtitle, string body)
    {
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"## {title}\n> **{subtitle}**\n{body}"));
    }

    private static void AddLevelCardSection(ContainerBuilder container, ulong targetUserId, ProfileLeague league, string rankText)
    {
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText(
            $"## {league.Emoji} {league.DisplayName}\n" +
            $"> **{AppLocalization.Format(LocalizationKeys.DiscordProfileLeagueRankValue, rankText)}**"));
        container.WithMediaGallery(new MediaGalleryBuilder()
            .AddItem($"attachment://{GetLevelCardFileName(targetUserId)}", league.DisplayName, false));
    }

    private static void AddAchievementPreviewSection(ContainerBuilder container, ulong targetUserId, int tradeCount, bool disableActions)
    {
        var earnedCount = GetEarnedAchievementCount(tradeCount);
        var button = new ButtonBuilder()
            .WithLabel(AppLocalization.Get(LocalizationKeys.DiscordProfileSeeAll))
            .WithCustomId("profile_view_badges")
            .WithStyle(ButtonStyle.Secondary)
            .WithDisabled(disableActions);

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithSection(new SectionBuilder()
            .AddComponent(new TextDisplayBuilder(TrimComponentText(
                $"## 🎖️ {AppLocalization.Get(LocalizationKeys.DiscordProfileLatestAchievements)}\n" +
                $"{earnedCount}/{AchievementMilestones.Length} {AppLocalization.Get(LocalizationKeys.DiscordProfileBadges).ToLowerInvariant()}")))
            .WithAccessory(button));
        container.WithMediaGallery(new MediaGalleryBuilder()
            .AddItem($"attachment://{GetAchievementPreviewFileName(targetUserId)}", AppLocalization.Get(LocalizationKeys.DiscordProfileLatestAchievements), false));
    }

    private static void AddAchievementCollectionSection(ContainerBuilder container, ulong targetUserId, int tradeCount, bool disableActions)
    {
        var earnedCount = GetEarnedAchievementCount(tradeCount);
        var button = new ButtonBuilder()
            .WithLabel(AppLocalization.Get(LocalizationKeys.DiscordProfileBackShort))
            .WithCustomId("profile_back_to_profile")
            .WithStyle(ButtonStyle.Secondary)
            .WithDisabled(disableActions);

        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithSection(new SectionBuilder()
            .AddComponent(new TextDisplayBuilder(TrimComponentText(
                $"## 🏅 {AppLocalization.Get(LocalizationKeys.DiscordProfileBadgeCase)}\n" +
                $"{earnedCount}/{AchievementMilestones.Length} {AppLocalization.Get(LocalizationKeys.DiscordProfileBadges).ToLowerInvariant()}")))
            .WithAccessory(button));
        container.WithMediaGallery(new MediaGalleryBuilder()
            .AddItem($"attachment://{GetAchievementCollectionFileName(targetUserId)}", AppLocalization.Get(LocalizationKeys.DiscordProfileBadgeCase), false));
    }

    private static void AddAboutSection(ContainerBuilder container, string body, bool canEdit, bool disableActions)
    {
        container.WithSeparator(SeparatorSpacingSize.Small, true);

        var content = new TextDisplayBuilder(TrimComponentText($"## 💬 {AppLocalization.Get(LocalizationKeys.DiscordProfileAboutMe)}\n{body}"));
        if (!canEdit)
        {
            container.WithTextDisplay(content);
            return;
        }

        var editButton = new ButtonBuilder()
            .WithLabel(AppLocalization.Get(LocalizationKeys.DiscordProfileEditBio))
            .WithCustomId(BioButtonCustomId)
            .WithStyle(ButtonStyle.Secondary)
            .WithDisabled(disableActions);

        container.WithSection(new SectionBuilder()
            .AddComponent(content)
            .WithAccessory(editButton));
    }

    private static string TrimComponentText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "\u200B";

        return text.Length <= MaxComponentTextLength ? text : text[..(MaxComponentTextLength - 3)] + "...";
    }

    private static Modal BuildBioModal(ulong targetUserId)
    {
        var currentBio = new TradeCodeStorage().GetTradeDetails(targetUserId)?.Quote ?? string.Empty;
        return new ModalBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordProfileBioModalTitle))
            .WithCustomId(GetBioModalCustomId(targetUserId))
            .AddTextInput(new TextInputBuilder()
                .WithLabel(AppLocalization.Get(LocalizationKeys.DiscordProfileBioModalLabel))
                .WithCustomId(BioInputCustomId)
                .WithStyle(TextInputStyle.Paragraph)
                .WithPlaceholder(AppLocalization.Get(LocalizationKeys.DiscordProfileBioModalPlaceholder))
                .WithValue(currentBio)
                .WithMaxLength(MaxBioLength)
                .WithRequired(false))
            .Build();
    }

    private static string GetBioModalCustomId(ulong targetUserId) => $"{BioModalPrefix}{targetUserId}";

    private static string GetLevelCardFileName(ulong targetUserId) => $"profile-level-card-{targetUserId}.png";

    private static string GetAchievementPreviewFileName(ulong targetUserId) => $"profile-achievements-{targetUserId}.png";

    private static string GetAchievementCollectionFileName(ulong targetUserId) => $"profile-achievement-collection-{targetUserId}.png";

    private async Task<SocketInteraction?> WaitForProfileInteractionAsync(IUserMessage message, ulong userId, ulong targetUserId, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<SocketInteraction?>(TaskCreationOptions.RunContinuationsAsynchronously);
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
                (component.Data.CustomId == "profile_view_badges" || component.Data.CustomId == "profile_back_to_profile" || component.Data.CustomId == BioButtonCustomId))
            {
                tcs.TrySetResult(component);
            }
            else if (interaction is SocketModal modal &&
                modal.User.Id == userId &&
                modal.Data.CustomId == GetBioModalCustomId(targetUserId))
            {
                tcs.TrySetResult(modal);
            }

            return Task.CompletedTask;
        }
    }

    private static string FormatStatLine(string label, string value) =>
        $"• **{label}:** {value}";

    private static string GetDisplayName(IUser user)
    {
        if (user is IGuildUser guildUser && !string.IsNullOrWhiteSpace(guildUser.Nickname))
            return guildUser.Nickname;

        if (!string.IsNullOrWhiteSpace(user.GlobalName))
            return user.GlobalName;

        return user.Username;
    }

    private sealed record ProfileLeague(
        string Key,
        string Emoji,
        string DisplayName,
        int RequiredLevel,
        Drawing.Color Primary,
        Drawing.Color Secondary,
        bool IsCup);

    private sealed record GlobalLeaderboardEntry(
        ulong UserId,
        int Level,
        int XP,
        int LeagueRequiredLevel);

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
            selected.RequiredLevel,
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

    private static string GetProgressBar(double percentage)
    {
        const int totalBlocks = 18;
        var filledBlocks = Math.Clamp((int)Math.Round(percentage / 100 * totalBlocks), 0, totalBlocks);
        return $"`{new string('█', filledBlocks)}{new string('░', totalBlocks - filledBlocks)}` **{percentage:0.0}%**";
    }

    private static int GetRequiredXPForNextLevel(int currentLevel) =>
        XpProgression.GetRequiredXPForNextLevel(currentLevel);

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

    private static string GetAboutText(TradeCodeStorage.TradeCodeDetails? tradeDetails)
    {
        if (!string.IsNullOrWhiteSpace(tradeDetails?.Quote))
            return $"> {tradeDetails.Quote.Trim()}";

        return $"> {AppLocalization.Get(LocalizationKeys.DiscordProfileNoBio)}";
    }

    private static int GetEarnedBadgeCount(int tradeCount) =>
        SysCordSettings.Settings.CustomBadgeEmojis.Count(b => tradeCount >= b.TradeCount);

    private static int GetEarnedAchievementCount(int tradeCount) =>
        AchievementMilestones.Count(milestone => tradeCount >= milestone);

    private static IReadOnlyList<int> GetAchievementPreviewMilestones(int tradeCount)
    {
        const int previewSlots = 4;
        var unlocked = AchievementMilestones.Where(milestone => tradeCount >= milestone).ToList();
        var upcoming = AchievementMilestones.Where(milestone => tradeCount < milestone).ToList();
        var selected = unlocked
            .TakeLast(Math.Min(previewSlots, unlocked.Count))
            .Concat(upcoming.Take(Math.Max(0, previewSlots - unlocked.Count)))
            .Take(previewSlots)
            .ToList();

        if (selected.Count < previewSlots)
            selected.InsertRange(0, AchievementMilestones.TakeLast(previewSlots - selected.Count));

        return selected;
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

    private static string GetAchievementPreviewText(int tradeCount)
    {
        const int previewSlots = 4;
        var earnedBadges = SysCordSettings.Settings.CustomBadgeEmojis
            .OrderByDescending(b => b.TradeCount)
            .Where(b => tradeCount >= b.TradeCount)
            .Take(previewSlots)
            .Select(b => b.Emoji)
            .ToList();

        var emptySlots = Enumerable.Repeat("◌", Math.Max(0, previewSlots - earnedBadges.Count));
        var badgeRow = string.Join("   ", earnedBadges.Concat(emptySlots));
        var nextBadge = SysCordSettings.Settings.CustomBadgeEmojis
            .OrderBy(b => b.TradeCount)
            .FirstOrDefault(b => b.TradeCount > tradeCount);
        var nextLine = nextBadge == null
            ? AppLocalization.Get(LocalizationKeys.DiscordProfileAllBadgesUnlocked)
            : AppLocalization.Format(LocalizationKeys.DiscordProfileTradesUntilBadge, (nextBadge.TradeCount - tradeCount).ToString("N0"), nextBadge.Emoji);

        return $"{badgeRow}\n{nextLine}";
    }

    private static int GetCommunityRankForUser(ulong userId, int currentLevel, int currentXp, ProfileLeague currentLeague, IReadOnlyCollection<SocketGuild> guilds)
    {
        var globalMemberCount = Math.Max(1, guilds.Sum(g => g.MemberCount));
        var scores = new Dictionary<ulong, (int Level, int XP)>
        {
            [userId] = (Math.Max(1, currentLevel), Math.Max(0, currentXp)),
        };

        if (File.Exists(StatsFilePath))
        {
            try
            {
                var json = File.ReadAllText(StatsFilePath);
                var stats = JsonSerializer.Deserialize<Dictionary<string, UserStats>>(json);
                if (stats != null)
                {
                    foreach (var (key, value) in stats)
                    {
                        if (!ulong.TryParse(key, out var id) || value == null)
                            continue;

                        if (id != userId && !IsKnownGlobalMember(id, guilds))
                            continue;

                        scores[id] = (Math.Max(1, value.Level), Math.Max(0, value.XP));
                    }
                }
            }
            catch
            {
                // Keep the current user ranked even if the persisted file is malformed.
            }
        }

        var currentScore = scores[userId];
        if (currentScore is { Level: <= 1, XP: <= 0 })
            return globalMemberCount;

        var knownRankedMembers = scores
            .Where(s => s.Key == userId || IsKnownGlobalMember(s.Key, guilds))
            .Select(s =>
            {
                var league = GetLeagueForLevel(s.Value.Level);
                return new GlobalLeaderboardEntry(s.Key, s.Value.Level, s.Value.XP, league.RequiredLevel);
            })
            .OrderByDescending(s => s.LeagueRequiredLevel)
            .ThenByDescending(s => s.Level)
            .ThenByDescending(s => s.XP)
            .ThenBy(s => s.UserId)
            .ToList();
        var rank = knownRankedMembers.FindIndex(s => s.UserId == userId) + 1;
        if (knownRankedMembers.Count > 0 && !CanHoldTopGlobalRank(currentLeague) && !CanHoldTopGlobalRank(knownRankedMembers[0].LeagueRequiredLevel))
            rank++;

        return rank <= 0 ? globalMemberCount : Math.Min(rank, globalMemberCount);
    }

    private static bool CanHoldTopGlobalRank(ProfileLeague league) =>
        league.RequiredLevel >= GetTopLeagueRequiredLevel();

    private static bool CanHoldTopGlobalRank(int requiredLevel) =>
        requiredLevel >= GetTopLeagueRequiredLevel();

    private static int GetTopLeagueRequiredLevel() =>
        SysCordSettings.Settings.LeagueEmojis.Count == 0
            ? 1
            : SysCordSettings.Settings.LeagueEmojis.Max(l => Math.Max(1, l.RequiredLevel));

    private static bool IsKnownGlobalMember(ulong userId, IReadOnlyCollection<SocketGuild> guilds) =>
        guilds.Any(g => g.GetUser(userId) != null);

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

    private static IReadOnlyList<(string Title, string Value)> GetBadgeRouteFields(int tradeCount)
    {
        const int maxFieldLength = 1000;
        var routeTitle = AppLocalization.Get(LocalizationKeys.DiscordProfileBadgeRoute);
        var badgeLines = SysCordSettings.Settings.CustomBadgeEmojis
            .OrderBy(b => b.TradeCount)
            .Select((b, index) =>
            {
                var state = tradeCount >= b.TradeCount ? "✅" : "⬛";
                var requirement = b.TradeCount == 1
                    ? AppLocalization.Format(LocalizationKeys.DiscordProfileBadgeSingular, b.Emoji, b.TradeCount)
                    : AppLocalization.Format(LocalizationKeys.DiscordProfileBadgePlural, b.Emoji, b.TradeCount);

                return $"{state} **{index + 1}.** {requirement}";
            })
            .ToList();

        if (badgeLines.Count == 0)
            return new List<(string Title, string Value)> { (routeTitle, AppLocalization.Get(LocalizationKeys.DiscordProfileNoBadges)) };

        var fields = new List<(string Title, string Value)>();
        var currentLines = new List<string>();
        var currentLength = 0;

        foreach (var line in badgeLines)
        {
            var nextLength = currentLength == 0 ? line.Length : currentLength + 1 + line.Length;
            if (currentLines.Count > 0 && nextLength > maxFieldLength)
            {
                var title = fields.Count == 0 ? routeTitle : $"{routeTitle} {fields.Count + 1}";
                fields.Add((title, string.Join("\n", currentLines)));
                currentLines.Clear();
                currentLength = 0;
            }

            currentLines.Add(line);
            currentLength = currentLength == 0 ? line.Length : currentLength + 1 + line.Length;
        }

        if (currentLines.Count > 0)
        {
            var title = fields.Count == 0 ? routeTitle : $"{routeTitle} {fields.Count + 1}";
            fields.Add((title, string.Join("\n", currentLines)));
        }

        return fields;
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
