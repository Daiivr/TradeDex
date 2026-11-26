using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Drawing.PokeSprite;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace SysBot.Pokemon.Discord;

public class DiscordTradeNotifier<T> : IPokeTradeNotifier<T>, IDisposable
    where T : PKM, new()
{
    private T Data { get; set; }
    private PokeTradeTrainerInfo Info { get; }
    private int Code { get; }
    private List<Pictocodes> LGCode { get; }
    private SocketUser Trader { get; }
    private int BatchTradeNumber { get; set; }
    private int TotalBatchTrades { get; }
    private bool IsMysteryEgg { get; }
    private bool IsMysteryTrade { get; }

    private readonly ulong _traderID;
    private int _uniqueTradeID;
    private Timer? _periodicUpdateTimer;
    private const int PeriodicUpdateInterval = 60000; // 60 seconds in milliseconds
    private bool _isTradeActive = true;
    private bool _initialUpdateSent = false;
    private bool _almostUpNotificationSent = false;
    private int _lastReportedPosition = -1;

    public readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

    public DiscordTradeNotifier(T data, PokeTradeTrainerInfo info, int code, SocketUser trader, int batchTradeNumber, int totalBatchTrades, bool isMysteryEgg, List<Pictocodes> lgcode, bool isMysteryTrade = false)
    {
        Data = data;
        Info = info;
        Code = code;
        Trader = trader;
        BatchTradeNumber = batchTradeNumber;
        TotalBatchTrades = totalBatchTrades;
        IsMysteryEgg = isMysteryEgg;
        IsMysteryTrade = isMysteryTrade;
        LGCode = lgcode;
        _traderID = trader.Id;
        _uniqueTradeID = GetUniqueTradeID();
    }

    public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }

    public void UpdateBatchProgress(int currentBatchNumber, T currentPokemon, int uniqueTradeID)
    {
        BatchTradeNumber = currentBatchNumber;
        Data = currentPokemon;
        _uniqueTradeID = uniqueTradeID;
    }

    public void UpdateUniqueTradeID(int uniqueTradeID)
    {
        _uniqueTradeID = uniqueTradeID;
    }

    private int GetUniqueTradeID()
    {
        // Generate a unique trade ID using timestamp or another method
        return (int)(DateTime.UtcNow.Ticks % int.MaxValue);
    }

    private void StartPeriodicUpdates()
    {
        // Dispose existing timer if it exists
        _periodicUpdateTimer?.Dispose();

        _isTradeActive = true;

        // Create a new timer that checks if user is up next
        // Only sends ONE notification when they're truly up next to avoid Discord spam
        _periodicUpdateTimer = new Timer(async _ =>
        {
            if (!_isTradeActive)
                return;

            try
            {
                // Check the current position using the unique trade ID
                var position = Hub.Queues.Info.CheckPosition(_traderID, _uniqueTradeID, PokeRoutineType.LinkTrade);
                if (!position.InQueue)
                    return;

                var currentPosition = position.Position < 1 ? 1 : position.Position;

                // Store the latest position for future reference
                _lastReportedPosition = currentPosition;

                var botct = Hub.Bots.Count;
                var currentETA = currentPosition > botct ? Hub.Config.Queues.EstimateDelay(currentPosition, botct) : 0;
                var waitText = currentETA > 0
                    ? AppLocalization.Format(LocalizationKeys.DiscordMinutes, currentETA)
                    : AppLocalization.Get(LocalizationKeys.DiscordLessThanMinute);

                if (position.InQueue && position.Detail != null)
                {
                    if (currentPosition <= 2 && _initialUpdateSent && !_almostUpNotificationSent)
                    {
                        _almostUpNotificationSent = true;

                        var batchInfo = TotalBatchTrades > 1
                            ? AppLocalization.Format(LocalizationKeys.DiscordUpNextBatchInfo, TotalBatchTrades)
                            : "";

                        var upNextEmbed = new EmbedBuilder
                        {
                            Color = Color.Gold,
                            Title = $"🎯 {AppLocalization.Get(LocalizationKeys.DiscordUpNextTitle)}",
                            Description = AppLocalization.Format(LocalizationKeys.DiscordUpNextDescription, currentPosition, batchInfo),
                            Footer = new EmbedFooterBuilder
                            {
                                Text = AppLocalization.Format(LocalizationKeys.DiscordUpNextFooter, waitText)
                            },
                            Timestamp = DateTimeOffset.Now
                        }.Build();

                        await Trader.SendMessageAsync(embed: upNextEmbed).ConfigureAwait(false);
                    }
                    else if (!position.Detail.Trade.IsProcessing && _initialUpdateSent && !_almostUpNotificationSent && currentPosition % 3 == 0)
                    {
                        var queueUpdateEmbed = new EmbedBuilder
                        {
                            Color = Color.Blue,
                            Title = $"📢 {AppLocalization.Get(LocalizationKeys.DiscordQueuePositionUpdateTitle)}",
                            Description = AppLocalization.Format(LocalizationKeys.DiscordQueuePositionUpdateDescription, currentPosition),
                            Footer = new EmbedFooterBuilder
                            {
                                Text = AppLocalization.Format(LocalizationKeys.DiscordEstimatedWaitFooter, waitText)
                            },
                            Timestamp = DateTimeOffset.Now
                        }.Build();

                        await Trader.SendMessageAsync(embed: queueUpdateEmbed).ConfigureAwait(false);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Discord client was disposed, stop periodic updates
                Base.LogUtil.LogError("Discord client disposed during periodic update. Stopping updates.", "StartPeriodicUpdates");
                StopPeriodicUpdates();
            }
            catch (Exception ex)
            {
                // Log any other errors but don't crash
                Base.LogUtil.LogError($"Unexpected error in periodic trade update: {ex.Message}", "StartPeriodicUpdates");
            }
        },
        null,
        PeriodicUpdateInterval, // Start after 60 seconds
        PeriodicUpdateInterval); // Repeat every 60 seconds
    }

    private void StopPeriodicUpdates()
    {
        _isTradeActive = false;
        _periodicUpdateTimer?.Dispose();
        _periodicUpdateTimer = null;
    }

    public async Task SendInitialQueueUpdate()
    {
        try
        {
            var position = Hub.Queues.Info.CheckPosition(_traderID, _uniqueTradeID, PokeRoutineType.LinkTrade);
            var currentPosition = position.Position < 1 ? 1 : position.Position;
            var botct = Hub.Bots.Count;
            var currentETA = currentPosition > botct ? Hub.Config.Queues.EstimateDelay(currentPosition, botct) : 0;

            _lastReportedPosition = currentPosition;

            var batchDescription = TotalBatchTrades > 1
                ? AppLocalization.Format(LocalizationKeys.DiscordBatchQueuedDescription, TotalBatchTrades, currentPosition)
                : AppLocalization.Format(LocalizationKeys.DiscordTradeQueuedDescription, currentPosition);

            var waitText = currentETA > 0
                ? AppLocalization.Format(LocalizationKeys.DiscordMinutes, currentETA)
                : AppLocalization.Get(LocalizationKeys.DiscordLessThanMinute);

            var initialEmbed = new EmbedBuilder
            {
                Color = Color.Green,
                Title = TotalBatchTrades > 1
                    ? $"🎁 {AppLocalization.Get(LocalizationKeys.DiscordBatchQueuedTitle)}"
                    : $"✅ {AppLocalization.Get(LocalizationKeys.DiscordTradeQueuedTitle)}",
                Description = batchDescription,
                Footer = new EmbedFooterBuilder
                {
                    Text = AppLocalization.Format(LocalizationKeys.DiscordEstimatedWaitFooter, waitText)
                },
                Timestamp = DateTimeOffset.Now
            }.Build();

            await Trader.SendMessageAsync(embed: initialEmbed).ConfigureAwait(false);

            _initialUpdateSent = true;

            // Start sending periodic updates about queue position
            StartPeriodicUpdates();
        }
        catch (ObjectDisposedException)
        {
            Base.LogUtil.LogError("Discord client disposed when sending initial queue update.", "SendInitialQueueUpdate");
        }
        catch (Exception ex)
        {
            Base.LogUtil.LogError($"Unexpected error sending initial queue update: {ex.Message}", "SendInitialQueueUpdate");
        }
    }

    public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        // Update unique trade ID from the detail
        _uniqueTradeID = info.UniqueTradeID;

        // Stop periodic updates as we're now moving to the active trading phase
        StopPeriodicUpdates();

        // Mark trade as active to prevent any further queue messages
        _almostUpNotificationSent = true;

        var speciesName = info.Type == PokeTradeType.Item
            ? GetDisplayItemName(Data.HeldItem)
            : GetDisplaySpeciesName(Data);
        if (IsMysteryEgg)
            speciesName = AppLocalization.Get(LocalizationKeys.DiscordMysteryEgg);
        else if (IsMysteryTrade)
            speciesName = AppLocalization.Get(LocalizationKeys.DiscordMysteryPokemon);

        var receive = IsMysteryEgg || IsMysteryTrade
            ? $" **({speciesName})**"
            : Data.Species == 0
                ? string.Empty
                : info.Type == PokeTradeType.Item
                    ? $" **({speciesName})**"
                    : $" **({Data.Nickname})**";

        if (Data is PK9)
        {
            string message;
            if (TotalBatchTrades > 1)
            {
                if (BatchTradeNumber == 1)
                {
                    message = AppLocalization.Format(LocalizationKeys.DiscordBatchTradeStarting, TotalBatchTrades, speciesName, receive);
                }
                else
                {
                    message = AppLocalization.Format(LocalizationKeys.DiscordBatchTradePreparing, BatchTradeNumber, TotalBatchTrades, speciesName, receive);
                }
            }
            else
            {
                message = AppLocalization.Format(LocalizationKeys.DiscordTradeInitializing, receive);
            }

            EmbedHelper.SendTradeInitializingEmbedAsync(Trader, speciesName, Code, IsMysteryEgg, message, IsMysteryTrade).ConfigureAwait(false);
        }
        else if (Data is PB7)
        {
            var (thefile, lgcodeembed) = CreateLGLinkCodeSpriteEmbed(LGCode);
            Trader.SendFileAsync(thefile, AppLocalization.Format(LocalizationKeys.DiscordLgTradeInitializing, receive), embed: lgcodeembed).ConfigureAwait(false);
        }
        else
        {
            EmbedHelper.SendTradeInitializingEmbedAsync(Trader, speciesName, Code, IsMysteryEgg, isMysteryTrade: IsMysteryTrade).ConfigureAwait(false);
        }
    }

    public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        // Ensure periodic updates are stopped (extra safety check)
        StopPeriodicUpdates();

        var name = Info.TrainerName;
        var trainer = string.IsNullOrEmpty(name) ? string.Empty : $" {name}";

        if (Data is PB7 && LGCode != null && LGCode.Count != 0)
        {
            var batchInfo = TotalBatchTrades > 1 ? $" (Trade {BatchTradeNumber}/{TotalBatchTrades})" : "";
            var message = AppLocalization.Format(LocalizationKeys.DiscordWaitingForUser, trainer, batchInfo, routine.InGameName);
            Trader.SendMessageAsync(message).ConfigureAwait(false);
        }
        else
        {
            string? additionalMessage = null;
            if (TotalBatchTrades > 1)
            {
                if (BatchTradeNumber == 1)
                {
                    additionalMessage = AppLocalization.Format(LocalizationKeys.DiscordBatchSelectFirst, TotalBatchTrades);
                }
                else
                {
                    var speciesName = IsMysteryEgg
                        ? AppLocalization.Get(LocalizationKeys.DiscordMysteryEgg)
                        : IsMysteryTrade
                            ? AppLocalization.Get(LocalizationKeys.DiscordMysteryPokemon)
                            : GetDisplaySpeciesName(Data);
                    additionalMessage = AppLocalization.Format(LocalizationKeys.DiscordBatchSelectNext, BatchTradeNumber, TotalBatchTrades, speciesName);
                }
            }

            EmbedHelper.SendTradeSearchingEmbedAsync(Trader, trainer, routine.InGameName, additionalMessage).ConfigureAwait(false);
        }
    }

    public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        OnFinish?.Invoke(routine);
        StopPeriodicUpdates();

        var cancelReason = msg.GetDescription();
        var cancelMessage = TotalBatchTrades > 1
            ? AppLocalization.Format(LocalizationKeys.DiscordBatchCanceled, cancelReason)
            : cancelReason;

        EmbedHelper.SendTradeCanceledEmbedAsync(Trader, cancelMessage).ConfigureAwait(false);
    }

    public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        // Only stop updates and invoke OnFinish for single trades or the last trade in a batch
        if (TotalBatchTrades <= 1 || BatchTradeNumber == TotalBatchTrades)
        {
            OnFinish?.Invoke(routine);
            StopPeriodicUpdates();
        }

        var tradedToUser = Data.Species;

        // Create different messages based on whether this is a single trade or part of a batch
        string message;
        if (TotalBatchTrades > 1)
        {
            if (BatchTradeNumber == TotalBatchTrades)
            {
                // Final trade in the batch - this is now called only once at the very end
                message = "✅ " + AppLocalization.Format(LocalizationKeys.DiscordBatchAllCompleted, TotalBatchTrades);
            }
            else
            {
                // Mid-batch trade
                var speciesName = IsMysteryEgg
                    ? AppLocalization.Get(LocalizationKeys.DiscordMysteryEgg)
                    : IsMysteryTrade
                        ? AppLocalization.Get(LocalizationKeys.DiscordMysteryPokemon)
                        : GetDisplaySpeciesName(Data);
                message = "✅ " + AppLocalization.Format(LocalizationKeys.DiscordBatchTradeCompleted, BatchTradeNumber, TotalBatchTrades, speciesName, BatchTradeNumber + 1);
            }
        }
        else
        {
            // Standard single trade message
            message = tradedToUser == 0
                ? AppLocalization.Get(LocalizationKeys.DiscordTradeFinished)
                : info.Type == PokeTradeType.Item
                    ? AppLocalization.Format(LocalizationKeys.DiscordTradeFinishedEnjoyPokemon, GetDisplayItemName(Data.HeldItem))
                : IsMysteryTrade
                    ? AppLocalization.Get(LocalizationKeys.DiscordTradeFinishedMysteryPokemon)
                    : IsMysteryEgg
                        ? AppLocalization.Get(LocalizationKeys.DiscordTradeFinishedMysteryEgg)
                        : AppLocalization.Format(LocalizationKeys.DiscordTradeFinishedEnjoyPokemon, GetDisplaySpeciesName(Data));
        }

        var storage = new TradeCodeStorage();
        storage.RecordLastTrade(Trader.Id, GetLastTradeDisplayName(info.Type));

        if (result is not null && result.Species > 0)
        {
            var partyBytes = new byte[result.SIZE_PARTY];
            result.WriteDecryptedDataParty(partyBytes);
            storage.RecordReceivedTradeFile(Trader.Id, result.FileName, GetDisplaySpeciesName(result), partyBytes);
        }

        EmbedHelper.SendTradeFinishedEmbedAsync(Trader, message, Data, IsMysteryEgg, IsMysteryTrade, info.Type).ConfigureAwait(false);

        // For single trades only, return the Pokemon immediately
        // Batch trades will have their Pokemon returned separately via SendNotification
        if (result is not null && Hub.Config.Discord.ReturnPKMs && TotalBatchTrades <= 1)
        {
            Trader.SendPKMAsync(result, AppLocalization.Get(LocalizationKeys.DiscordReturnPokemon)).ConfigureAwait(false);
        }
    }

    private string GetLastTradeDisplayName(PokeTradeType type)
    {
        if (IsMysteryEgg)
            return AppLocalization.Get(LocalizationKeys.DiscordMysteryEgg);
        if (IsMysteryTrade)
            return AppLocalization.Get(LocalizationKeys.DiscordMysteryPokemon);
        if (type == PokeTradeType.Item)
            return GetDisplayItemName(Data.HeldItem);
        if (Data.Species == 0)
            return AppLocalization.Get(LocalizationKeys.DiscordUnknown);

        return GetDisplaySpeciesName(Data);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
    {
        message = AppLocalization.LocalizeRuntimeMessage(message);

        // Add batch context to notifications if applicable
        if (TotalBatchTrades > 1 && !message.Contains("Trade") && !message.Contains("batch"))
        {
            message = AppLocalization.Format(LocalizationKeys.DiscordTradeBatchPrefix, BatchTradeNumber, TotalBatchTrades, message);
        }

        EmbedHelper.SendNotificationEmbedAsync(Trader, message).ConfigureAwait(false);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
    {
        if (message.ExtraInfo is SeedSearchResult r)
        {
            SendNotificationZ3(r);
            return;
        }

        var msg = message.Summary;
        if (message.Details.Count > 0)
            msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
        Trader.SendMessageAsync(msg).ConfigureAwait(false);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        message = AppLocalization.LocalizeRuntimeMessage(message);

        // Always send the Pokemon if requested, regardless of trade type
        if (result.Species != 0 && (Hub.Config.Discord.ReturnPKMs || info.Type == PokeTradeType.Dump))
        {
            Trader.SendPKMAsync(result, message).ConfigureAwait(false);
        }
    }

    private void SendNotificationZ3(SeedSearchResult r)
    {
        var lines = r.ToString();

        var embed = new EmbedBuilder { Color = Color.LighterGrey };
        embed.AddField(x =>
        {
            x.Name = AppLocalization.Format(LocalizationKeys.DiscordSeedFieldName, r.Seed);
            x.Value = lines;
            x.IsInline = false;
        });
        var msg = AppLocalization.Format(LocalizationKeys.DiscordSeedDetails, r.Seed);
        Trader.SendMessageAsync(msg, embed: embed.Build()).ConfigureAwait(false);
    }

    private static string GetDisplayLanguageCode() => AppLocalization.Language switch
    {
        AppLanguage.Spanish => "es",
        _ => "en",
    };

    private static string GetDisplaySpeciesName(T pk)
    {
        var strings = GameInfo.GetStrings(GetDisplayLanguageCode());
        return pk.Species > 0 && pk.Species < strings.Species.Count
            ? strings.Species[pk.Species]
            : ((Species)pk.Species).ToString();
    }

    private static string GetDisplayItemName(int item)
    {
        if (item <= 0)
            return AppLocalization.Get(LocalizationKeys.DiscordNoHeldItem);

        var strings = GameInfo.GetStrings(GetDisplayLanguageCode());
        return item < strings.Item.Count ? strings.Item[item] : item.ToString();
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
            codecount++;
        }
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
        var filename = $"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png";
        outputImage.Save(filename);
        filename = System.IO.Path.GetFileName($"{System.IO.Directory.GetCurrentDirectory()}//finalcode.png");
        Embed returnembed = new EmbedBuilder().WithTitle($"{lgcode[0]}, {lgcode[1]}, {lgcode[2]}").WithImageUrl($"attachment://{filename}").Build();
        return (filename, returnembed);
    }

    public void Dispose()
    {
        StopPeriodicUpdates();
        GC.SuppressFinalize(this);
    }

    ~DiscordTradeNotifier()
    {
        Dispose();
    }
}
