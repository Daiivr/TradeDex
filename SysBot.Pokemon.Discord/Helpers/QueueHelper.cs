using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Drawing.PokeSprite;
using SysBot.Pokemon.Discord.Commands.Bots;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord;

public static class QueueHelper<T> where T : PKM, new()
{
    private const uint MaxTradeCode = 9999_9999;
    private const string MysteryTradeImageUrl = "https://i.imgur.com/FdESYAv.png";

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
        { 700, "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/700.png" }
    };

    private static GameStrings GetDisplayStrings() => GameInfo.GetStrings(AppLocalization.Language switch
    {
        AppLanguage.Spanish => "es",
        _ => "en",
    });

    private static string GetDisplayItemName(int item)
    {
        if (item <= 0)
            return AppLocalization.Get(LocalizationKeys.DiscordNoHeldItem);

        var strings = GetDisplayStrings();
        return item < strings.Item.Count ? strings.Item[item] : item.ToString();
    }

    private static string GetTradingBotUrl()
    {
        var configuredUrl = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ExtraEmbedOptions.TradingBotUrl;
        return string.IsNullOrWhiteSpace(configuredUrl) ? "https://zepkm.com/pokecreator" : configuredUrl;
    }

    private static string GetNonNativeNoticeText()
    {
        var configuredText = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ExtraEmbedOptions.NonNativeTexT;
        return string.IsNullOrWhiteSpace(configuredText)
            ? AppLocalization.Get(LocalizationKeys.DiscordCannotEnterHomeAutoOt)
            : configuredText;
    }

    private static string GetHomeTrackerInfo(IHomeTrack homeTrack)
    {
        return SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowTracker
            ? $"\n\n**Home Tracker:** ||{homeTrack.Tracker}||"
            : string.Empty;
    }

    private static string BuildQueueFooter(int totalTradeCount, TradeCodeStorage.TradeCodeDetails? tradeDetails, string trainerMention, int position, double eta, int batchTradeNumber, int totalBatchTrades)
    {
        var footerParts = new List<string>();
        if (totalTradeCount > 0)
            footerParts.Add(AppLocalization.Format(LocalizationKeys.DiscordUserTradesCompact, totalTradeCount));

        footerParts.Add(AppLocalization.Format(LocalizationKeys.DiscordCurrentQueuePosition, position == -1 ? 1 : position));

        string userDetailsText = DetailsExtractor<T>.GetUserDetails(totalTradeCount, tradeDetails, trainerMention);
        if (!string.IsNullOrWhiteSpace(userDetailsText))
            footerParts.Add(userDetailsText);

        footerParts.Add(AppLocalization.Format(LocalizationKeys.DiscordWaitEstimateTradeNumber, eta, batchTradeNumber, totalBatchTrades));
        return string.Join("\n", footerParts);
    }

    private static string GetMilestoneDescription(int tradeCount)
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
            _ => AppLocalization.Format(LocalizationKeys.DiscordMilestoneDefault, tradeCount)
        };
    }

    public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser trader, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isHiddenTrade = false, bool isMysteryEgg = false, List<Pictocodes>? lgcode = null, bool ignoreAutoOT = false, bool setEdited = false, bool isNonNative = false, bool isMysteryTrade = false)
    {
        if ((uint)code > MaxTradeCode)
        {
            await context.Channel.SendMessageAsync(AppLocalization.Get(LocalizationKeys.DiscordTradeCodeRange)).ConfigureAwait(false);
            return;
        }

        try
        {
            // Only send trade code for non-batch trades (batch container will handle its own)
            if (!isBatchTrade)
            {
                if (trade is PB7 && lgcode != null)
                {
                    var (thefile, lgcodeembed) = CreateLGLinkCodeSpriteEmbed(lgcode);
                    await trader.SendFileAsync(thefile, "Your trade code will be.", embed: lgcodeembed).ConfigureAwait(false);
                }
                else
                {
                    await EmbedHelper.SendTradeCodeEmbedAsync(trader, code).ConfigureAwait(false);
                }
            }

            var result = await AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, isBatchTrade, batchTradeNumber, totalBatchTrades, isHiddenTrade, isMysteryEgg, lgcode, ignoreAutoOT, setEdited, isNonNative, isMysteryTrade).ConfigureAwait(false);
        }
        catch (HttpException ex)
        {
            await HandleDiscordExceptionAsync(context, trader, ex).ConfigureAwait(false);
        }
    }

    public static Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, bool ignoreAutoOT = false)
    {
        return AddToQueueAsync(context, code, trainer, sig, trade, routine, type, context.User, ignoreAutoOT: ignoreAutoOT);
    }

    private static async Task<TradeQueueResult> AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName,
        RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, bool isBatchTrade,
        int batchTradeNumber, int totalBatchTrades, bool isHiddenTrade, bool isMysteryEgg = false,
        List<Pictocodes>? lgcode = null, bool ignoreAutoOT = false, bool setEdited = false, bool isNonNative = false, bool isMysteryTrade = false)
    {
        // Note: This method should only be called for individual trades now
        // Batch trades use AddBatchContainerToQueueAsync

        var user = trader;
        var userID = user.Id;
        var name = user.Username;
        var trainer = new PokeTradeTrainerInfo(trainerName, userID);
        var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, trader, batchTradeNumber, totalBatchTrades,
            isMysteryEgg, lgcode: lgcode!, isMysteryTrade: isMysteryTrade);

        int uniqueTradeID = GenerateUniqueTradeID();

        var detail = new PokeTradeDetail<T>(pk, trainer, notifier, t, code, sig == RequestSignificance.Favored,
            lgcode, batchTradeNumber, totalBatchTrades, isMysteryEgg, isHiddenTrade, uniqueTradeID, ignoreAutoOT, setEdited, isMysteryTrade);

        var trade = new TradeEntry<T>(detail, userID, PokeRoutineType.LinkTrade, name, uniqueTradeID);
        var hub = SysCord<T>.Runner.Hub;
        var Info = hub.Queues.Info;
        var isSudo = sig == RequestSignificance.Owner;
        var added = Info.AddToTradeQueue(trade, userID, false, isSudo);

        // Start queue position updates for Discord notification
        if (added != QueueResultAdd.AlreadyInQueue && added != QueueResultAdd.NotAllowedItem && notifier is DiscordTradeNotifier<T> discordNotifier)
        {
            // IMPORTANT: Update the notifier's unique trade ID to match the one used in the queue
            // Otherwise the DM will check position with the wrong ID and return incorrect results
            discordNotifier.UpdateUniqueTradeID(uniqueTradeID);
            await discordNotifier.SendInitialQueueUpdate().ConfigureAwait(false);
        }

        int totalTradeCount = 0;
        TradeCodeStorage.TradeCodeDetails? tradeDetails = null;
        if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            totalTradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            tradeDetails = tradeCodeStorage.GetTradeDetails(trader.Id);
        }

        if (added == QueueResultAdd.AlreadyInQueue)
        {
            await Helpers<T>.SendAlreadyInQueueEmbedAsync(context).ConfigureAwait(false);
            return new TradeQueueResult(false);
        }

        if (added == QueueResultAdd.QueueFull)
        {
            await SendQueueFullEmbedAsync(context).ConfigureAwait(false);
            return new TradeQueueResult(false);
        }

        if (added == QueueResultAdd.NotAllowedItem)
        {
            var held = pk.HeldItem;
            var itemName = GetDisplayItemName(held);
            await context.Channel.SendMessageAsync(AppLocalization.Format(LocalizationKeys.DiscordTradeBlockedHeldItemPlza, trader.Mention, itemName)).ConfigureAwait(false);
            return new TradeQueueResult(false);
        }

        var embedData = DetailsExtractor<T>.ExtractPokemonDetails(
            pk, trader, isMysteryEgg, type == PokeRoutineType.Clone, type == PokeRoutineType.Dump,
            type == PokeRoutineType.FixOT, type == PokeRoutineType.SeedCheck, false, 1, 1
        );

        try
        {
            (string embedImageUrl, DiscordColor embedColor) = t == PokeTradeType.Item
                ? (string.Empty, DiscordColor.Gold)
                : await PrepareEmbedDetails(pk, isMysteryEgg);

            embedData.EmbedImageUrl = isMysteryTrade ? MysteryTradeImageUrl :
            type == PokeRoutineType.Dump ? "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/Dumping.png?raw=true&width=300&height=300" :
            type == PokeRoutineType.Clone ? "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/Cloning.png?raw=true&width=300&height=300" :
            type == PokeRoutineType.SeedCheck ? "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/Seeding.png?raw=true&width=300&height=300" :
            type == PokeRoutineType.FixOT ? "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/FixOTing.png?raw=true&width=300&height=300" :
                                       embedImageUrl;

            embedData.HeldItemUrl = string.Empty;
            if (pk.HeldItem > 0)
            {
                string heldItemName = GameInfo.GetStrings("en").itemlist[pk.HeldItem]
                    .ToLower()
                    .Replace(" ", "")
                    .Replace("é", "e");
                embedData.HeldItemUrl = t == PokeTradeType.Item
                    ? $"https://serebii.net/itemdex/sprites/sv/{heldItemName}.png"
                    : $"https://serebii.net/itemdex/sprites/{heldItemName}.png";
            }

            embedData.IsLocalFile = File.Exists(embedData.EmbedImageUrl);
            if (t == PokeTradeType.Item)
                embedData.AuthorName = AppLocalization.Format(LocalizationKeys.DiscordItemRequestedBy, trader.Username);

            var position = Info.CheckPosition(userID, uniqueTradeID, type);
            var botct = Info.Hub.Bots.Count;
            var baseEta = position.Position > botct ? Info.Hub.Config.Queues.EstimateDelay(position.Position, botct) : 0;
            string trainerMention = trader.Mention;
            string footerText = BuildQueueFooter(totalTradeCount, tradeDetails, trainerMention, position.Position, baseEta, batchTradeNumber, totalBatchTrades);

            var embedBuilder = new EmbedBuilder()
                .WithColor(t == PokeTradeType.Item ? DiscordColor.Gold : embedColor)
                .WithFooter(footerText)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(embedData.AuthorName)
                    .WithIconUrl(trader.GetAvatarUrl() ?? trader.GetDefaultAvatarUrl())
                    .WithUrl(GetTradingBotUrl()));

            if (t == PokeTradeType.Item && !string.IsNullOrEmpty(embedData.HeldItemUrl))
            {
                embedBuilder.WithImageUrl(embedData.HeldItemUrl);
            }
            else
            {
                embedBuilder.WithImageUrl(embedData.IsLocalFile ? $"attachment://{Path.GetFileName(embedData.EmbedImageUrl)}" : embedData.EmbedImageUrl);
            }

            if (t == PokeTradeType.Item)
            {
                var itemInfo = string.IsNullOrWhiteSpace(embedData.HeldItem)
                    ? AppLocalization.Get(LocalizationKeys.DiscordNoHeldItem)
                    : embedData.HeldItem;
                embedBuilder.AddField("\u200B", $"**{AppLocalization.Get(LocalizationKeys.DiscordTrainerLabel)}:** {trader.Mention}\n{itemInfo}", inline: false);
            }
            else
            {
                DetailsExtractor<T>.AddAdditionalText(embedBuilder);

                if (!isMysteryTrade && !isMysteryEgg && type != PokeRoutineType.Clone && type != PokeRoutineType.Dump && type != PokeRoutineType.FixOT && type != PokeRoutineType.SeedCheck)
                {
                    DetailsExtractor<T>.AddNormalTradeFields(embedBuilder, embedData, trader.Mention, pk);
                }
                else
                {
                    DetailsExtractor<T>.AddSpecialTradeFields(embedBuilder, isMysteryEgg, type == PokeRoutineType.SeedCheck, type == PokeRoutineType.Clone, type == PokeRoutineType.FixOT, trader.Mention, isMysteryTrade);
                }
            }

            // Check if the Pokemon is Non-Native and/or has a Home Tracker
            if (pk is IHomeTrack homeTrack)
            {
                if (homeTrack.HasTracker && isNonNative)
                {
                    embedBuilder.Footer.IconUrl = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/exclamation.gif";
                    embedBuilder.AddField($"{AppLocalization.Get(LocalizationKeys.DiscordNotice)}: {AppLocalization.Get(LocalizationKeys.DiscordNonNativeHomeTrackerNotice)}", AppLocalization.Get(LocalizationKeys.DiscordAutoOtNotApplied) + GetHomeTrackerInfo(homeTrack));
                }
                else if (homeTrack.HasTracker)
                {
                    embedBuilder.Footer.IconUrl = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/exclamation.gif";
                    embedBuilder.AddField($"{AppLocalization.Get(LocalizationKeys.DiscordNotice)}: {AppLocalization.Get(LocalizationKeys.DiscordHomeTrackerNotice)}", AppLocalization.Get(LocalizationKeys.DiscordAutoOtNotApplied) + GetHomeTrackerInfo(homeTrack));
                }
                else if (isNonNative)
                {
                    embedBuilder.Footer.IconUrl = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/exclamation.gif";
                    embedBuilder.AddField($"{AppLocalization.Get(LocalizationKeys.DiscordNotice)}: {AppLocalization.Get(LocalizationKeys.DiscordNonNativeNotice)}", GetNonNativeNoticeText());
                }
            }
            else if (isNonNative)
            {
                embedBuilder.Footer.IconUrl = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/exclamation.gif";
                embedBuilder.AddField($"{AppLocalization.Get(LocalizationKeys.DiscordNotice)}: {AppLocalization.Get(LocalizationKeys.DiscordNonNativeNotice)}", GetNonNativeNoticeText());
            }

            if (!isMysteryTrade && t != PokeTradeType.Item)
                DetailsExtractor<T>.AddThumbnails(embedBuilder, type == PokeRoutineType.Clone, type == PokeRoutineType.SeedCheck, embedData.HeldItemUrl, pk, t);

            if (!isHiddenTrade && SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.UseEmbeds)
            {
                var embed = embedBuilder.Build();
                if (embed == null)
                {
                    Console.WriteLine(AppLocalization.LocalizeRuntimeMessage("Error: Embed is null."));
                    await context.Channel.SendMessageAsync(AppLocalization.Get(LocalizationKeys.DiscordTradeDetailsPrepareError));
                    return new TradeQueueResult(false);
                }

                if (embedData.IsLocalFile)
                {
                    await context.Channel.SendFileAsync(embedData.EmbedImageUrl, embed: embed);
                    await ScheduleFileDeletion(embedData.EmbedImageUrl, 0);
                }
                else
                {
                    await context.Channel.SendMessageAsync(embed: embed);
                }
            }
            else
            {
                var species = string.IsNullOrWhiteSpace(embedData.SpeciesName) || embedData.SpeciesName == "---"
                    ? AppLocalization.Get(LocalizationKeys.BotStatusUnknown)
                    : embedData.SpeciesName;
                var message = AppLocalization.Format(LocalizationKeys.DiscordHiddenTradeAdded, position.Position, species, baseEta);
                await context.Channel.SendMessageAsync(message);
            }
        }
        catch (HttpException ex)
        {
            await HandleDiscordExceptionAsync(context, trader, ex);
            return new TradeQueueResult(false);
        }

        if (SysCord<T>.Runner.Hub.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            int tradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            _ = SendMilestoneEmbed(tradeCount, context.Channel, trader);
        }

        return new TradeQueueResult(true);
    }

    public static async Task AddBatchContainerToQueueAsync(SocketCommandContext context, int code, string trainer, T firstTrade, List<T> allTrades, RequestSignificance sig, SocketUser trader, int totalBatchTrades, string? customAuthorTitle = null, bool isMysteryTrade = false)
    {
        var userID = trader.Id;
        var name = trader.Username;
        var trainer_info = new PokeTradeTrainerInfo(trainer, userID);
        var notifier = new DiscordTradeNotifier<T>(firstTrade, trainer_info, code, trader, 1, totalBatchTrades, false, lgcode: [], isMysteryTrade);

        int uniqueTradeID = GenerateUniqueTradeID();

        var detail = new PokeTradeDetail<T>(firstTrade, trainer_info, notifier, PokeTradeType.Batch, code,
            sig == RequestSignificance.Favored, null, 1, totalBatchTrades, false, isMysteryTrade: isMysteryTrade)
        {
            BatchTrades = allTrades
        };

        var trade = new TradeEntry<T>(detail, userID, PokeRoutineType.Batch, name, uniqueTradeID: uniqueTradeID);
        var hub = SysCord<T>.Runner.Hub;
        var Info = hub.Queues.Info;
        var added = Info.AddToTradeQueue(trade, userID, false, sig == RequestSignificance.Owner);

        // Send trade code once
        await EmbedHelper.SendTradeCodeEmbedAsync(trader, code).ConfigureAwait(false);

        // Start queue position updates for Discord notification
        if (added != QueueResultAdd.AlreadyInQueue && added != QueueResultAdd.NotAllowedItem && notifier is DiscordTradeNotifier<T> discordNotifier)
        {
            // IMPORTANT: Update the notifier's unique trade ID to match the one used in the queue
            // Otherwise the DM will check position with the wrong ID and return incorrect results
            discordNotifier.UpdateUniqueTradeID(uniqueTradeID);
            await discordNotifier.SendInitialQueueUpdate().ConfigureAwait(false);
        }

        // Handle the display
        if (added == QueueResultAdd.AlreadyInQueue)
        {
            await Helpers<T>.SendAlreadyInQueueEmbedAsync(context).ConfigureAwait(false);
            return;
        }

        if (added == QueueResultAdd.QueueFull)
        {
            await SendQueueFullEmbedAsync(context).ConfigureAwait(false);
            return;
        }

        if (added == QueueResultAdd.NotAllowedItem)
        {
            var held = firstTrade.HeldItem;
            var itemName = GetDisplayItemName(held);
            await context.Channel.SendMessageAsync(AppLocalization.Format(LocalizationKeys.DiscordTradeBlockedHeldItemPlza, trader.Mention, itemName)).ConfigureAwait(false);
            return;
        }

        var position = Info.CheckPosition(userID, uniqueTradeID, PokeRoutineType.Batch);
        var botct = Info.Hub.Bots.Count;
        var baseEta = position.Position > botct ? Info.Hub.Config.Queues.EstimateDelay(position.Position, botct) : 0;

        // Get user trade details for footer
        int totalTradeCount = 0;
        TradeCodeStorage.TradeCodeDetails? tradeDetails = null;
        if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            totalTradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            tradeDetails = tradeCodeStorage.GetTradeDetails(trader.Id);
        }

        // Send initial batch summary message
        var queueSummaryKey = isMysteryTrade
            ? LocalizationKeys.DiscordMysteryMonBatchAddedSummary
            : LocalizationKeys.DiscordBatchAddedSummary;
        await context.Channel.SendMessageAsync(AppLocalization.Format(queueSummaryKey, trader.Mention, totalBatchTrades, position.Position, baseEta)).ConfigureAwait(false);

        // Create and send one compact embed for the full batch instead of one embed per Pokemon.
        if (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.UseEmbeds)
        {
            try
            {
                string? batchImagePath = null;
                var embedColor = DiscordColor.Gold;
                if (!isMysteryTrade)
                {
                    batchImagePath = await CreateBatchPreviewImageAsync(allTrades).ConfigureAwait(false);
                    (string firstImageUrl, embedColor) = await PrepareEmbedDetails(firstTrade).ConfigureAwait(false);
                    if (File.Exists(firstImageUrl))
                        await ScheduleFileDeletion(firstImageUrl, 0).ConfigureAwait(false);
                }

                var authorName = string.IsNullOrWhiteSpace(customAuthorTitle)
                    ? AppLocalization.Format(LocalizationKeys.DiscordBatchSummaryAuthor, trader.Username)
                    : $"{trader.Username}'s {customAuthorTitle}";

                var footerText = BuildBatchSummaryFooter(totalTradeCount, tradeDetails, trader.Mention, position.Position, baseEta, totalBatchTrades);
                var description = isMysteryTrade
                    ? AppLocalization.Format(LocalizationKeys.DiscordMysteryMonBatchDescription, totalBatchTrades)
                    : BuildBatchSummaryDescription(allTrades);

                var embedBuilder = new EmbedBuilder()
                    .WithColor(embedColor)
                    .WithDescription(description)
                    .WithImageUrl(isMysteryTrade ? MysteryTradeImageUrl : $"attachment://{Path.GetFileName(batchImagePath)}")
                    .WithFooter(footerText)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(authorName)
                        .WithIconUrl(trader.GetAvatarUrl() ?? trader.GetDefaultAvatarUrl())
                        .WithUrl(GetTradingBotUrl()));

                DetailsExtractor<T>.AddAdditionalText(embedBuilder);

                if (!isMysteryTrade && allTrades.OfType<IHomeTrack>().Any(z => z.HasTracker))
                {
                    embedBuilder.Footer.IconUrl = "https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/exclamation.gif";
                    embedBuilder.AddField($"{AppLocalization.Get(LocalizationKeys.DiscordNotice)}: {AppLocalization.Get(LocalizationKeys.DiscordHomeTrackerNotice)}", AppLocalization.Get(LocalizationKeys.DiscordAutoOtNotApplied));
                }

                if (isMysteryTrade)
                {
                    await context.Channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
                }
                else
                {
                    await context.Channel.SendFileAsync(batchImagePath!, embed: embedBuilder.Build()).ConfigureAwait(false);
                    await ScheduleFileDeletion(batchImagePath!, 0).ConfigureAwait(false);
                }
            }
            catch (HttpException ex)
            {
                await HandleDiscordExceptionAsync(context, trader, ex);
            }
        }

        // Send milestone embed if applicable
        if (SysCord<T>.Runner.Hub.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            int tradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            _ = SendMilestoneEmbed(tradeCount, context.Channel, trader);
        }
    }

    public static async Task AddMysteryEggBatchContainerToQueueAsync(SocketCommandContext context, int code, string trainer, T firstTrade, List<T> allTrades, RequestSignificance sig, SocketUser trader, int totalBatchTrades)
    {
        var userID = trader.Id;
        var name = trader.Username;
        var trainer_info = new PokeTradeTrainerInfo(trainer, userID);
        var notifier = new DiscordTradeNotifier<T>(firstTrade, trainer_info, code, trader, 1, totalBatchTrades, true, lgcode: []);

        int uniqueTradeID = GenerateUniqueTradeID();

        var detail = new PokeTradeDetail<T>(firstTrade, trainer_info, notifier, PokeTradeType.Batch, code,
            sig == RequestSignificance.Favored, null, 1, totalBatchTrades, true)
        {
            BatchTrades = allTrades
        };

        var trade = new TradeEntry<T>(detail, userID, PokeRoutineType.Batch, name, uniqueTradeID: uniqueTradeID);
        var hub = SysCord<T>.Runner.Hub;
        var Info = hub.Queues.Info;
        var added = Info.AddToTradeQueue(trade, userID, false, sig == RequestSignificance.Owner);

        await EmbedHelper.SendTradeCodeEmbedAsync(trader, code).ConfigureAwait(false);

        if (added != QueueResultAdd.AlreadyInQueue && added != QueueResultAdd.NotAllowedItem && notifier is DiscordTradeNotifier<T> discordNotifier)
        {
            discordNotifier.UpdateUniqueTradeID(uniqueTradeID);
            await discordNotifier.SendInitialQueueUpdate().ConfigureAwait(false);
        }

        if (added == QueueResultAdd.AlreadyInQueue)
        {
            await Helpers<T>.SendAlreadyInQueueEmbedAsync(context).ConfigureAwait(false);
            return;
        }

        if (added == QueueResultAdd.QueueFull)
        {
            await SendQueueFullEmbedAsync(context).ConfigureAwait(false);
            return;
        }

        if (added == QueueResultAdd.NotAllowedItem)
        {
            var held = firstTrade.HeldItem;
            var blockedItemName = GetDisplayItemName(held);
            await context.Channel.SendMessageAsync(AppLocalization.Format(LocalizationKeys.DiscordTradeBlockedHeldItemPlza, trader.Mention, blockedItemName)).ConfigureAwait(false);
            return;
        }

        var position = Info.CheckPosition(userID, uniqueTradeID, PokeRoutineType.Batch);
        var botct = Info.Hub.Bots.Count;
        var baseEta = position.Position > botct ? Info.Hub.Config.Queues.EstimateDelay(position.Position, botct) : 0;

        int totalTradeCount = 0;
        TradeCodeStorage.TradeCodeDetails? tradeDetails = null;
        if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            totalTradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            tradeDetails = tradeCodeStorage.GetTradeDetails(trader.Id);
        }

        if (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.UseEmbeds)
        {
            try
            {
                string mysteryEggImageUrl = GetMysteryEggTypeImageUrl(firstTrade);
                string description = AppLocalization.Format(LocalizationKeys.DiscordMysteryEggBatchDescription, totalBatchTrades, totalBatchTrades == 1 ? string.Empty : "s");

                string footerText = BuildQueueFooter(totalTradeCount, tradeDetails, trader.Mention, position.Position, baseEta, 1, totalBatchTrades);

                var embedBuilder = new EmbedBuilder()
                    .WithColor(DiscordColor.Gold)
                    .WithImageUrl(mysteryEggImageUrl)
                    .WithDescription(description)
                    .WithFooter(footerText)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(AppLocalization.Format(LocalizationKeys.DiscordMysteryEggBatchAuthor, trader.Username))
                        .WithIconUrl(trader.GetAvatarUrl() ?? trader.GetDefaultAvatarUrl())
                        .WithUrl(GetTradingBotUrl()));

                DetailsExtractor<T>.AddAdditionalText(embedBuilder);

                await context.Channel.SendMessageAsync(embed: embedBuilder.Build());
            }
            catch (HttpException ex)
            {
                await HandleDiscordExceptionAsync(context, trader, ex);
            }
        }
        else
        {
            await context.Channel.SendMessageAsync(AppLocalization.Format(LocalizationKeys.DiscordMysteryEggBatchAddedPlain, trader.Mention, totalBatchTrades, position.Position, baseEta)).ConfigureAwait(false);
        }

        if (SysCord<T>.Runner.Hub.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            int tradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            _ = SendMilestoneEmbed(tradeCount, context.Channel, trader);
        }
    }

    public static async Task AddItemBatchContainerToQueueAsync(SocketCommandContext context, int code, string trainer, T firstTrade, List<T> allTrades, RequestSignificance sig, SocketUser trader, int totalBatchTrades)
    {
        var userID = trader.Id;
        var name = trader.Username;
        var trainer_info = new PokeTradeTrainerInfo(trainer, userID);
        var notifier = new DiscordTradeNotifier<T>(firstTrade, trainer_info, code, trader, 1, totalBatchTrades, false, lgcode: []);

        int uniqueTradeID = GenerateUniqueTradeID();

        var detail = new PokeTradeDetail<T>(firstTrade, trainer_info, notifier, PokeTradeType.Batch, code,
            sig == RequestSignificance.Favored, null, 1, totalBatchTrades, false)
        {
            BatchTrades = allTrades
        };

        var trade = new TradeEntry<T>(detail, userID, PokeRoutineType.Batch, name, uniqueTradeID: uniqueTradeID);
        var hub = SysCord<T>.Runner.Hub;
        var Info = hub.Queues.Info;
        var added = Info.AddToTradeQueue(trade, userID, false, sig == RequestSignificance.Owner);

        // Send trade code once
        await EmbedHelper.SendTradeCodeEmbedAsync(trader, code).ConfigureAwait(false);

        if (added != QueueResultAdd.AlreadyInQueue && added != QueueResultAdd.NotAllowedItem && notifier is DiscordTradeNotifier<T> discordNotifier)
        {
            discordNotifier.UpdateUniqueTradeID(uniqueTradeID);
            await discordNotifier.SendInitialQueueUpdate().ConfigureAwait(false);
        }

        if (added == QueueResultAdd.AlreadyInQueue)
        {
            await Helpers<T>.SendAlreadyInQueueEmbedAsync(context).ConfigureAwait(false);
            return;
        }

        if (added == QueueResultAdd.QueueFull)
        {
            await SendQueueFullEmbedAsync(context).ConfigureAwait(false);
            return;
        }

        if (added == QueueResultAdd.NotAllowedItem)
        {
            var held = firstTrade.HeldItem;
            var blockedItemName = GetDisplayItemName(held);
            await context.Channel.SendMessageAsync(AppLocalization.Format(LocalizationKeys.DiscordTradeBlockedHeldItemPlza, trader.Mention, blockedItemName)).ConfigureAwait(false);
            return;
        }

        var position = Info.CheckPosition(userID, uniqueTradeID, PokeRoutineType.Batch);
        var botct = Info.Hub.Bots.Count;
        var baseEta = position.Position > botct ? Info.Hub.Config.Queues.EstimateDelay(position.Position, botct) : 0;

        int totalTradeCount = 0;
        TradeCodeStorage.TradeCodeDetails? tradeDetails = null;
        if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            totalTradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            tradeDetails = tradeCodeStorage.GetTradeDetails(trader.Id);
        }

        if (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.UseEmbeds)
        {
            try
            {
                var displayStrings = GetDisplayStrings();
                var englishStrings = PKHeX.Core.GameInfo.GetStrings("en");
                string itemDisplayName = firstTrade.HeldItem > 0 ? displayStrings.itemlist[firstTrade.HeldItem] : AppLocalization.Get(LocalizationKeys.DiscordNoHeldItem);
                string heldItemKey = firstTrade.HeldItem > 0 ? englishStrings.itemlist[firstTrade.HeldItem].ToLower().Replace(" ", "") : string.Empty;
                string itemImageUrl = $"https://serebii.net/itemdex/sprites/sv/{heldItemKey}.png";
                string speciesName = displayStrings.Species[firstTrade.Species];
                bool canGmax = firstTrade is PK8 pk8 && pk8.CanGigantamax;
                string speciesImageUrl = TradeExtensions<T>.PokeImg(firstTrade, canGmax, false, SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.PreferredImageSize);

                string pluralSuffix = totalBatchTrades == 1 ? string.Empty : "s";
                string description = AppLocalization.Format(LocalizationKeys.DiscordItemBatchDescription, speciesName, totalBatchTrades, itemDisplayName, pluralSuffix);

                string footerText = BuildQueueFooter(totalTradeCount, tradeDetails, trader.Mention, position.Position, baseEta, 1, totalBatchTrades);

                var embedBuilder = new EmbedBuilder()
                    .WithColor(DiscordColor.Gold)
                    .WithImageUrl(itemImageUrl)
                    .WithThumbnailUrl(speciesImageUrl)
                    .WithDescription(description)
                    .WithFooter(footerText)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(AppLocalization.Format(LocalizationKeys.DiscordItemBatchAuthor, trader.Username))
                        .WithIconUrl(trader.GetAvatarUrl() ?? trader.GetDefaultAvatarUrl())
                        .WithUrl(GetTradingBotUrl()));

                DetailsExtractor<T>.AddAdditionalText(embedBuilder);

                await context.Channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                await HandleDiscordExceptionAsync(context, trader, ex);
            }
        }
        else
        {
            await context.Channel.SendMessageAsync(AppLocalization.Format(LocalizationKeys.DiscordItemBatchAddedPlain, trader.Mention, totalBatchTrades, position.Position, baseEta)).ConfigureAwait(false);
        }

        if (SysCord<T>.Runner.Hub.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            int tradeCount = tradeCodeStorage.GetTradeCount(trader.Id);
            _ = SendMilestoneEmbed(tradeCount, context.Channel, trader);
        }
    }

    private static Task SendQueueFullEmbedAsync(SocketCommandContext context)
    {
        var maxCount = SysCord<T>.Runner.Config.Queues.MaxQueueCount;
        var embed = new EmbedBuilder()
            .WithColor(DiscordColor.Red)
            .WithTitle($"🚫 {AppLocalization.Get(LocalizationKeys.DiscordQueueFullTitle)}")
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordQueueFullDescription, maxCount))
            .WithFooter(AppLocalization.Get(LocalizationKeys.DiscordQueueFullFooter))
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        return context.Channel.SendMessageAsync(embed: embed);
    }

    private static int GenerateUniqueTradeID()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomValue = Random.Shared.Next(1000);
        return (int)((timestamp % int.MaxValue) * 1000 + randomValue);
    }

    private static string GetImageFolderPath()
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string imagesFolder = Path.Combine(baseDirectory, "Images");

        if (!Directory.Exists(imagesFolder))
        {
            Directory.CreateDirectory(imagesFolder);
        }

        return imagesFolder;
    }

    private static string SaveImageLocally(System.Drawing.Image image)
    {
        string imagesFolderPath = GetImageFolderPath();
        string filePath = Path.Combine(imagesFolderPath, $"image_{Guid.NewGuid()}.png");

#pragma warning disable CA1416 // Validate platform compatibility
        image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
#pragma warning restore CA1416 // Validate platform compatibility

        return filePath;
    }

    private static string BuildBatchSummaryDescription(IReadOnlyList<T> trades)
    {
        var lines = new List<string>(trades.Count);
        for (int i = 0; i < trades.Count; i++)
            lines.Add($"{i + 1}. {GetBatchPokemonDisplayName(trades[i])}");

        return string.Join("\n", lines);
    }

    private static string GetBatchPokemonDisplayName(T pk)
    {
        var strings = GetDisplayStrings();
        var speciesName = pk.Species > 0 && pk.Species < strings.Species.Count
            ? strings.Species[pk.Species]
            : ((Species)pk.Species).ToString();

        var shiny = pk.IsShiny ? "✨ " : string.Empty;
        var gender = GetBatchGenderDisplay(pk);

        return string.IsNullOrWhiteSpace(gender)
            ? $"{shiny}{speciesName}"
            : $"{shiny}{speciesName} {gender}";
    }

    private static string GetBatchGenderDisplay(T pk)
    {
        var genderSymbol = pk.Gender < GameInfo.GenderSymbolASCII.Count
            ? GameInfo.GenderSymbolASCII[pk.Gender]
            : string.Empty;

        var genderEmojis = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.GenderEmojis;
        return genderSymbol switch
        {
            "M" => !string.IsNullOrWhiteSpace(genderEmojis.MaleEmoji.EmojiString) ? genderEmojis.MaleEmoji.EmojiString : "(M)",
            "F" => !string.IsNullOrWhiteSpace(genderEmojis.FemaleEmoji.EmojiString) ? genderEmojis.FemaleEmoji.EmojiString : "(F)",
            _ => string.Empty,
        };
    }

    private static string BuildBatchSummaryFooter(int totalTradeCount, TradeCodeStorage.TradeCodeDetails? tradeDetails, string trainerMention, int position, double eta, int totalBatchTrades)
    {
        var footerParts = new List<string>();
        if (totalTradeCount > 0)
            footerParts.Add(AppLocalization.Format(LocalizationKeys.DiscordUserTradesCompact, totalTradeCount));

        footerParts.Add(AppLocalization.Format(LocalizationKeys.DiscordCurrentQueuePosition, position == -1 ? 1 : position));
        footerParts.Add(AppLocalization.Format(LocalizationKeys.DiscordBatchSummaryFooter, totalBatchTrades));

        string userDetailsText = DetailsExtractor<T>.GetUserDetails(totalTradeCount, tradeDetails, trainerMention);
        if (!string.IsNullOrWhiteSpace(userDetailsText))
            footerParts.Add(userDetailsText);

        footerParts.Add(AppLocalization.Format(LocalizationKeys.DiscordWaitEstimateTradeNumber, eta, 1, totalBatchTrades));
        return string.Join("\n", footerParts);
    }

    private static async Task<string> CreateBatchPreviewImageAsync(IReadOnlyList<T> trades)
    {
        const int cellSize = 132;
        const int spriteBox = 110;
        const int padding = 20;
        const int maxColumns = 6;

        var columns = Math.Max(1, Math.Min(maxColumns, trades.Count));
        var rows = Math.Max(1, (int)Math.Ceiling(trades.Count / (double)columns));
        var width = padding + columns * cellSize + padding;
        var height = padding + rows * cellSize + padding;

#pragma warning disable CA1416 // Validate platform compatibility
        using var canvas = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(canvas))
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            for (int i = 0; i < trades.Count; i++)
            {
                var pk = trades[i];
                var col = i % columns;
                var row = i / columns;
                var x = padding + col * cellSize;
                var y = padding + row * cellSize;

                bool canGmax = pk is PK8 pk8 && pk8.CanGigantamax;
                var spriteUrl = TradeExtensions<T>.PokeImg(pk, canGmax, false, SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.PreferredImageSize);
                using var sprite = await LoadImageFromUrl(spriteUrl).ConfigureAwait(false);
                if (sprite != null)
                    DrawImageContained(graphics, sprite, new Rectangle(x, y, spriteBox, spriteBox));

                using var ball = await LoadImageFromUrl(GetBallImageUrl(pk)).ConfigureAwait(false);
                if (ball != null)
                    graphics.DrawImage(ball, x + spriteBox - ball.Width + 4, y + spriteBox - ball.Height + 4, ball.Width, ball.Height);
            }
        }

        var filePath = SaveImageLocally(canvas);
#pragma warning restore CA1416 // Validate platform compatibility
        return filePath;
    }

    private static void DrawImageContained(Graphics graphics, System.Drawing.Image image, Rectangle bounds)
    {
        var ratio = Math.Min((double)bounds.Width / image.Width, (double)bounds.Height / image.Height);
        var width = (int)(image.Width * ratio);
        var height = (int)(image.Height * ratio);
        var x = bounds.X + (bounds.Width - width) / 2;
        var y = bounds.Y + (bounds.Height - height) / 2;
        graphics.DrawImage(image, x, y, width, height);
    }

    private static string GetBallImageUrl(T pk)
    {
        var strings = GameInfo.GetStrings("en");
        string ballName = strings.balllist[pk.Ball];
        if (ballName.Contains("(LA)"))
            ballName = "la" + ballName.Replace(" ", "").Replace("(LA)", "").ToLower();
        else
            ballName = ballName.Replace(" ", "").ToLower();

        return $"https://raw.githubusercontent.com/hexbyt3/sprites/main/AltBallImg/20x20/{ballName}.png";
    }

    public static async Task<(string, DiscordColor)> PrepareEmbedDetails(T pk, bool isMysteryEgg = false)
    {
        string embedImageUrl;
        string speciesImageUrl;

        if (pk.IsEgg || isMysteryEgg)
        {
            string eggImageUrl = isMysteryEgg ? GetMysteryEggTypeImageUrl(pk) : GetEggTypeImageUrl(pk);
            if (isMysteryEgg)
            {
                embedImageUrl = eggImageUrl;
                speciesImageUrl = eggImageUrl;
            }
            else
            {
                speciesImageUrl = TradeExtensions<T>.PokeImg(pk, false, true, null);
                System.Drawing.Image combinedImage = await OverlaySpeciesOnEgg(eggImageUrl, speciesImageUrl);
                embedImageUrl = SaveImageLocally(combinedImage);
                speciesImageUrl = embedImageUrl;
            }
        }
        else
        {
            bool canGmax = pk is PK8 pk8 && pk8.CanGigantamax;
            speciesImageUrl = TradeExtensions<T>.PokeImg(pk, canGmax, false, SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.PreferredImageSize);
            embedImageUrl = speciesImageUrl;
        }

        if (!isMysteryEgg)
        {
            var strings = GameInfo.GetStrings("en");
            string ballName = strings.balllist[pk.Ball];
            if (ballName.Contains("(LA)"))
            {
                ballName = "la" + ballName.Replace(" ", "").Replace("(LA)", "").ToLower();
            }
            else
            {
                ballName = ballName.Replace(" ", "").ToLower();
            }

            string ballImgUrl = $"https://raw.githubusercontent.com/hexbyt3/sprites/main/AltBallImg/20x20/{ballName}.png";

            if (Uri.TryCreate(embedImageUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeFile)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                using var localImage = await Task.Run(() => System.Drawing.Image.FromFile(uri.LocalPath));
#pragma warning restore CA1416 // Validate platform compatibility
                using var ballImage = await LoadImageFromUrl(ballImgUrl);
                if (ballImage != null)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    using (var graphics = Graphics.FromImage(localImage))
                    {
                        var ballPosition = new Point(localImage.Width - ballImage.Width, localImage.Height - ballImage.Height);
                        graphics.DrawImage(ballImage, ballPosition);
                    }
#pragma warning restore CA1416 // Validate platform compatibility
                    embedImageUrl = SaveImageLocally(localImage);
                }
            }
            else
            {
                (System.Drawing.Image? finalCombinedImage, bool ballImageLoaded) = await OverlayBallOnSpecies(speciesImageUrl, ballImgUrl);
                if (finalCombinedImage != null)
                {
                    embedImageUrl = SaveImageLocally(finalCombinedImage);
                }
                else
                {
                    embedImageUrl = speciesImageUrl;
                }

                if (!ballImageLoaded)
                {
                    Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"Ball image could not be loaded: {ballImgUrl}"));
                }
            }
        }

        (int R, int G, int B) = await GetDominantColorAsync(embedImageUrl);
        return (embedImageUrl, new DiscordColor(R, G, B));
    }

    private static async Task<(System.Drawing.Image?, bool)> OverlayBallOnSpecies(string speciesImageUrl, string ballImageUrl)
    {
        using var speciesImage = await LoadImageFromUrl(speciesImageUrl);
        if (speciesImage == null)
        {
            Console.WriteLine(AppLocalization.LocalizeRuntimeMessage("Species image could not be loaded."));
            return (null, false);
        }

        var ballImage = await LoadImageFromUrl(ballImageUrl);
        if (ballImage == null)
        {
            Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"Ball image could not be loaded: {ballImageUrl}"));
#pragma warning disable CA1416 // Validate platform compatibility
            return ((System.Drawing.Image)speciesImage.Clone(), false);
#pragma warning restore CA1416 // Validate platform compatibility
        }

        using (ballImage)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            using (var graphics = Graphics.FromImage(speciesImage))
            {
                var ballPosition = new Point(speciesImage.Width - ballImage.Width, speciesImage.Height - ballImage.Height);
                graphics.DrawImage(ballImage, ballPosition);
            }
#pragma warning restore CA1416 // Validate platform compatibility

#pragma warning disable CA1416 // Validate platform compatibility
            return ((System.Drawing.Image)speciesImage.Clone(), true);
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }

    private static async Task<System.Drawing.Image> OverlaySpeciesOnEgg(string eggImageUrl, string speciesImageUrl)
    {
        System.Drawing.Image? eggImage = await LoadImageFromUrl(eggImageUrl);
        System.Drawing.Image? speciesImage = await LoadImageFromUrl(speciesImageUrl);
        
        if (eggImage == null || speciesImage == null)
        {
            throw new InvalidOperationException("Failed to load egg or species image.");
        }

#pragma warning disable CA1416 // Validate platform compatibility
        double scaleRatio = Math.Min((double)eggImage.Width / speciesImage.Width, (double)eggImage.Height / speciesImage.Height);
        Size newSize = new((int)(speciesImage.Width * scaleRatio), (int)(speciesImage.Height * scaleRatio));
        System.Drawing.Image resizedSpeciesImage = new Bitmap(speciesImage, newSize);

        using (Graphics g = Graphics.FromImage(eggImage))
        {
            int speciesX = (eggImage.Width - resizedSpeciesImage.Width) / 2;
            int speciesY = (eggImage.Height - resizedSpeciesImage.Height) / 2;
            g.DrawImage(resizedSpeciesImage, speciesX, speciesY, resizedSpeciesImage.Width, resizedSpeciesImage.Height);
        }

        speciesImage.Dispose();
        resizedSpeciesImage.Dispose();

        double scale = Math.Min(128.0 / eggImage.Width, 128.0 / eggImage.Height);
        int newWidth = (int)(eggImage.Width * scale);
        int newHeight = (int)(eggImage.Height * scale);

        Bitmap finalImage = new(128, 128);

        using (Graphics g = Graphics.FromImage(finalImage))
        {
            int x = (128 - newWidth) / 2;
            int y = (128 - newHeight) / 2;
            g.DrawImage(eggImage, x, y, newWidth, newHeight);
        }

        eggImage.Dispose();
#pragma warning restore CA1416 // Validate platform compatibility
        return finalImage;
    }

    private static async Task<System.Drawing.Image?> LoadImageFromUrl(string url)
    {
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"Failed to load image from {url}. Status code: {response.StatusCode}"));
            return null;
        }

        Stream stream = await response.Content.ReadAsStreamAsync();
        if (stream == null || stream.Length == 0)
        {
            Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"No data or empty stream received from {url}"));
            return null;
        }

        try
        {
#pragma warning disable CA1416 // Validate platform compatibility
            return System.Drawing.Image.FromStream(stream);
#pragma warning restore CA1416 // Validate platform compatibility
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"Failed to create image from stream. URL: {url}, Exception: {ex}"));
            return null;
        }
    }

    public static async Task ScheduleFileDeletion(string filePath, int delayInMilliseconds)
    {
        await Task.Delay(delayInMilliseconds);
        DeleteFile(filePath);
    }

    private static void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"Error deleting file: {ex.Message}"));
            }
        }
    }

    private static async Task SendMilestoneEmbed(int tradeCount, ISocketMessageChannel channel, SocketUser user)
    {
        if (MilestoneImages.TryGetValue(tradeCount, out string? imageUrl))
        {
            var embed = new EmbedBuilder()
                .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordMilestoneMedalTitle, user.Username))
                .WithDescription(GetMilestoneDescription(tradeCount))
                .WithColor(new DiscordColor(255, 215, 0)) // Gold color
                .WithThumbnailUrl(imageUrl)
                .Build();

            await channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }
    }

    public static async Task<(int R, int G, int B)> GetDominantColorAsync(string imagePath)
    {
        try
        {
            Bitmap image = await LoadImageAsync(imagePath);

            var colorCount = new Dictionary<Color, int>();
#pragma warning disable CA1416 // Validate platform compatibility
            await Task.Run(() =>
            {
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        var pixelColor = image.GetPixel(x, y);

                        if (pixelColor.A < 128 || pixelColor.GetBrightness() > 0.9) continue;

                        var brightnessFactor = (int)(pixelColor.GetBrightness() * 100);
                        var saturationFactor = (int)(pixelColor.GetSaturation() * 100);
                        var combinedFactor = brightnessFactor + saturationFactor;

                        var quantizedColor = Color.FromArgb(
                            pixelColor.R / 10 * 10,
                            pixelColor.G / 10 * 10,
                            pixelColor.B / 10 * 10
                        );

                        if (colorCount.ContainsKey(quantizedColor))
                        {
                            colorCount[quantizedColor] += combinedFactor;
                        }
                        else
                        {
                            colorCount[quantizedColor] = combinedFactor;
                        }
                    }
                }
            });

            image.Dispose();
#pragma warning restore CA1416 // Validate platform compatibility

            if (colorCount.Count == 0)
                return (255, 255, 255);

            var dominantColor = colorCount.Aggregate((a, b) => a.Value > b.Value ? a : b).Key;
            return (dominantColor.R, dominantColor.G, dominantColor.B);
        }
        catch (Exception ex)
        {
            Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"Error processing image from {imagePath}. Error: {ex.Message}"));
            return (255, 255, 255);
        }
    }

    private static async Task<Bitmap> LoadImageAsync(string imagePath)
    {
        if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(imagePath);
            await using var stream = await response.Content.ReadAsStreamAsync();
#pragma warning disable CA1416 // Validate platform compatibility
            return new Bitmap(stream);
#pragma warning restore CA1416 // Validate platform compatibility
        }
        else
        {
#pragma warning disable CA1416 // Validate platform compatibility
            return new Bitmap(imagePath);
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }

    private static async Task HandleDiscordExceptionAsync(SocketCommandContext context, SocketUser trader, HttpException ex)
    {
        string message = string.Empty;
        switch (ex.DiscordCode)
        {
            case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
                {
                    var permissions = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);
                    if (!permissions.SendMessages)
                    {
                        message = AppLocalization.Get(LocalizationKeys.DiscordSendMessagesPermission);
                        Base.LogUtil.LogError("QueueHelper", message);
                        return;
                    }
                    if (!permissions.ManageMessages)
                    {
                        var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                        var owner = app.Owner.Id;
                        message = AppLocalization.Format(LocalizationKeys.DiscordManageMessagesPermission, owner);
                    }
                }
                break;

            case DiscordErrorCode.CannotSendMessageToUser:
                {
                    message = context.User == trader
                        ? AppLocalization.Get(LocalizationKeys.DiscordDmRequiredSelf)
                        : AppLocalization.Get(LocalizationKeys.DiscordDmRequiredMentioned);
                }
                break;

            default:
                {
                    message = ex.DiscordCode != null
                        ? AppLocalization.Format(LocalizationKeys.DiscordDiscordError, (int)ex.DiscordCode, ex.Reason)
                        : AppLocalization.Format(LocalizationKeys.DiscordHttpError, (int)ex.HttpCode, ex.Message);
                }
                break;
        }
        await context.Channel.SendMessageAsync(message).ConfigureAwait(false);
    }

    private static string GetEggTypeImageUrl(T pk)
    {
        var pi = pk.PersonalInfo;
        byte typeIndex = pi.Type1;

        string[] typeNames = [
            "Normal", "Fighting", "Flying", "Poison", "Ground", "Rock", "Bug", "Ghost",
            "Steel", "Fire", "Water", "Grass", "Electric", "Psychic", "Ice", "Dragon",
            "Dark", "Fairy"
        ];

        string typeName = (typeIndex >= 0 && typeIndex < typeNames.Length)
            ? typeNames[typeIndex]
            : "Normal";

        return $"https://raw.githubusercontent.com/Daiivr/SysBot-Images/refs/heads/main/Eggs2/Egg_{typeName}.png";
    }

    private static string GetMysteryEggTypeImageUrl(T pk)
    {
        var pi = pk.PersonalInfo;
        byte typeIndex = pi.Type1;

        string[] typeNames = [
            "Normal", "Fighting", "Flying", "Poison", "Ground", "Rock", "Bug", "Ghost",
            "Steel", "Fire", "Water", "Grass", "Electric", "Psychic", "Ice", "Dragon",
            "Dark", "Fairy"
        ];

        string typeName = typeIndex < typeNames.Length ? typeNames[typeIndex] : "Normal";
        return $"https://raw.githubusercontent.com/Daiivr/SysBot-Images/refs/heads/main/MysteryEggs/MEgg_{typeName}.png";
    }

    public static (string, Embed) CreateLGLinkCodeSpriteEmbed(List<Pictocodes> lgcode)
    {
        int codecount = 0;
        List<System.Drawing.Image> spritearray = [];
        foreach (Pictocodes cd in lgcode)
        {
            var showdown = new ShowdownSet(cd.ToString());
            var sav = BlankSaveFile.Get(EntityContext.Gen7b, "pip");
            PKM pk = sav.GetLegalFromSet(showdown).Created;
#pragma warning disable CA1416 // Validate platform compatibility
            var sprite = pk.Sprite();
            var destRect = new Rectangle(-40, -65, 137, 130);
            var destImage = new Bitmap(137, 130);

            destImage.SetResolution(sprite.HorizontalResolution, sprite.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(sprite, destRect, 0, 0, sprite.Width, sprite.Height, GraphicsUnit.Pixel);
            }
            sprite.Dispose();
            spritearray.Add(destImage);
#pragma warning restore CA1416 // Validate platform compatibility
            codecount++;
        }

#pragma warning disable CA1416 // Validate platform compatibility
        int outputImageWidth = spritearray[0].Width + 20;
        int outputImageHeight = spritearray[0].Height - 65;

        using var outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (Graphics graphics = Graphics.FromImage(outputImage))
        {
            graphics.DrawImage(spritearray[0], new Rectangle(0, 0, spritearray[0].Width, spritearray[0].Height),
                new Rectangle(new Point(), spritearray[0].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[1], new Rectangle(50, 0, spritearray[1].Width, spritearray[1].Height),
                new Rectangle(new Point(), spritearray[1].Size), GraphicsUnit.Pixel);
            graphics.DrawImage(spritearray[2], new Rectangle(100, 0, spritearray[2].Width, spritearray[2].Height),
                new Rectangle(new Point(), spritearray[2].Size), GraphicsUnit.Pixel);
        }
        foreach (var img in spritearray)
            img.Dispose();
        var filename = $"{Directory.GetCurrentDirectory()}//finalcode.png";
        outputImage.Save(filename);
#pragma warning restore CA1416 // Validate platform compatibility

        filename = Path.GetFileName($"{Directory.GetCurrentDirectory()}//finalcode.png");
        Embed returnembed = new EmbedBuilder().WithTitle($"{lgcode[0]}, {lgcode[1]}, {lgcode[2]}").WithImageUrl($"attachment://{filename}").Build();
        return (filename, returnembed);
    }
}
