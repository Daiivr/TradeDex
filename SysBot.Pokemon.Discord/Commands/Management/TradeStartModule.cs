using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord;

public class TradeStartModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private class TradeStartAction(
        ulong ChannelId,
        Action<PokeRoutineExecutorBase, PokeTradeDetail<T>> messager,
        string channel)
        : ChannelAction<PokeRoutineExecutorBase, PokeTradeDetail<T>>(ChannelId, messager, channel);

    private static DiscordSocketClient? _discordClient;

    private static readonly Dictionary<ulong, TradeStartAction> Channels = [];

    private static readonly HashSet<string> _startedTrades = [];

    private static void Remove(TradeStartAction entry)
    {
        Channels.Remove(entry.ChannelID);
        SysCord<T>.Runner.Hub.Queues.Forwarders.Remove(entry.Action);
    }

#pragma warning disable RCS1158
    public static void RestoreTradeStarting(DiscordSocketClient discord)
    {
        _discordClient = discord;

        var cfg = SysCordSettings.Settings;
        foreach (var ch in cfg.TradeStartingChannels)
        {
            if (discord.GetChannel(ch.ID) is ISocketMessageChannel c)
                AddLogChannel(c, ch.ID);
        }

        LogUtil.LogInfo("Discord", "Added Trade Start Notification to Discord channel(s) on Bot startup.");
    }
#pragma warning restore RCS1158

    public static bool IsStartChannel(ulong cid) => Channels.ContainsKey(cid);

    [Command("startHere")]
    [Summary("Makes the bot log trade starts to the channel.")]
    [RequireSudo]
    public async Task AddLogAsync()
    {
        var c = Context.Channel;
        var cid = c.Id;

        if (Channels.ContainsKey(cid))
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordLogAlreadyHere)).ConfigureAwait(false);
            return;
        }

        AddLogChannel(c, cid);

        SysCordSettings.Settings.TradeStartingChannels
            .AddIfNew([GetReference(Context.Channel)]);

        await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordStartNotificationAdded)).ConfigureAwait(false);
    }

    private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
    {
        async void Logger(PokeRoutineExecutorBase bot, PokeTradeDetail<T> detail)
        {
            if (detail.Type == PokeTradeType.Random)
                return;

            // Prevent duplicate embeds for the same trade in the same channel.
            var startedTradeKey = $"{cid}:{detail.ID}";
            lock (_startedTrades)
            {
                if (_startedTrades.Contains(startedTradeKey))
                    return;

                _startedTrades.Add(startedTradeKey);
            }

#pragma warning disable CS8602
            var user = _discordClient?.GetUser(detail.Trainer.ID);
#pragma warning restore CS8602
            if (user == null)
                return;

            string speciesName = detail.TradeData != null ? GetDisplaySpeciesName(detail.TradeData) : "";

            string ballImgUrl;

            if (detail.IsMysteryEgg || detail.IsMysteryTrade)
            {
                ballImgUrl = "https://i.imgur.com/kwm6A2D.png";
            }
            else
            {
                ballImgUrl = "https://raw.githubusercontent.com/hexbyt3/sprites/36e891cc02fe283cd70d9fc8fef2f3c490096d6c/imgs/difficulty.png";

                if (detail.TradeData != null &&
                    detail.Type is not (PokeTradeType.Clone or PokeTradeType.Dump or PokeTradeType.Seed or PokeTradeType.FixOT))
                {
                    var ballName = GameInfo.GetStrings("en").balllist[detail.TradeData.Ball]
                        .Replace(" ", "")
                        .Replace("(LA)", "")
                        .ToLower();

                    ballName = ballName == "pokéball"
                        ? "pokeball"
                        : ballName.Contains("(la)") ? "la" + ballName.Replace("(la)", "") : ballName;

                    ballImgUrl = $"https://raw.githubusercontent.com/hexbyt3/sprites/main/AltBallImg/28x28/{ballName}.png";
                }
            }

            string tradeTitle;
            string embedImageUrl;

            if (detail.Type == PokeTradeType.Item && detail.TradeData is not null)
            {
                tradeTitle = GetDisplayItemName(detail.TradeData.HeldItem);
                embedImageUrl = GetItemImageUrl(detail.TradeData.HeldItem);
                ballImgUrl = TradeExtensions<T>.PokeImg(detail.TradeData, false, true);
            }
            else
            {
                tradeTitle = detail.IsMysteryTrade
                    ? AppLocalization.Get(LocalizationKeys.DiscordMysteryPokemon)
                    : detail.IsMysteryEgg
                        ? AppLocalization.Get(LocalizationKeys.DiscordTradeMysteryEgg)
                        : detail.Type switch
                        {
                            PokeTradeType.Clone => AppLocalization.Get(LocalizationKeys.DiscordTradeClonedPokemon),
                            PokeTradeType.Dump => AppLocalization.Get(LocalizationKeys.DiscordTradeDump),
                            PokeTradeType.FixOT => AppLocalization.Get(LocalizationKeys.DiscordTradeFixOt),
                            PokeTradeType.Seed => AppLocalization.Get(LocalizationKeys.DiscordTradeSpecialRequest),
                            _ => speciesName
                        };

                embedImageUrl = detail.IsMysteryTrade
                    ? "https://i.imgur.com/FdESYAv.png"
                    : detail.IsMysteryEgg && detail.TradeData is not null
                        ? GetMysteryEggTypeImageUrl(detail.TradeData)
                        : detail.Type switch
                    {
                        PokeTradeType.Clone => "https://i.imgur.com/aSTCjUn.png",
                        PokeTradeType.Dump => "https://i.imgur.com/9wfEHwZ.png",
                        PokeTradeType.FixOT => "https://i.imgur.com/gRZGFIi.png",
                        PokeTradeType.Seed => "https://i.imgur.com/EI1BHr5.png",
                        _ => TradeExtensions<T>.PokeImg(detail.TradeData!, false, false, SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.PreferredImageSize)
                    };
            }

            var (r, g, b) = await GetDominantColorAsync(embedImageUrl);

            string footerText =
                detail.Type is PokeTradeType.Clone or PokeTradeType.Dump or PokeTradeType.Seed or PokeTradeType.FixOT
                    ? AppLocalization.Get(LocalizationKeys.DiscordTradeStarting)
                    : AppLocalization.Format(LocalizationKeys.DiscordTradeStartingWithTitle, tradeTitle);

            string authorText;
            string? authorIconUrl;

            if (detail.IsHiddenTrade)
            {
                authorText = AppLocalization.Get(LocalizationKeys.DiscordTradeUpNextHidden);
                authorIconUrl = "https://i.imgur.com/pTqYqXP.gif";
            }
            else
            {
                authorText = AppLocalization.Format(LocalizationKeys.DiscordTradeUpNextUser, user.Username);
                authorIconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
            }

            var embed = new EmbedBuilder()
                .WithColor(new DiscordColor(r, g, b))
                .WithThumbnailUrl(embedImageUrl)
                .WithAuthor(authorText, authorIconUrl)
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordTradeStartDescription, tradeTitle, detail.ID))
                .WithFooter($"{footerText}\u200B", ballImgUrl)
                .WithTimestamp(DateTime.Now)
                .Build();

            try
            {
                await c.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch (HttpException ex) when (ex.HttpCode is System.Net.HttpStatusCode.ServiceUnavailable
                                                       or System.Net.HttpStatusCode.GatewayTimeout
                                                       or System.Net.HttpStatusCode.BadGateway)
            {
                // Discord is temporarily unavailable; skip this notification rather than crashing.
                LogUtil.LogError(AppLocalization.Format(LocalizationKeys.LogTradeStartSkipped, (int)ex.HttpCode, ex.Message), "TradeStartModule");
            }
            catch (Exception ex)
            {
                LogUtil.LogError(AppLocalization.Format(LocalizationKeys.LogTradeStartFailed, ex.Message), "TradeStartModule");
            }
        }

        SysCord<T>.Runner.Hub.Queues.Forwarders.Add(Logger);
        Channels[cid] = new TradeStartAction(cid, Logger, c.Name);
    }

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = AppLocalization.Format(LocalizationKeys.DiscordReferenceAddedBy, Context.User.Username, DateTime.Now),
    };

    private static string GetDisplayLanguageCode() => AppLocalization.Language switch
    {
        AppLanguage.Spanish => "es",
        _ => "en",
    };

    private static string GetDisplaySpeciesName(PKM pk)
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

    private static string GetItemImageUrl(int item)
    {
        if (item <= 0)
            return string.Empty;

        var itemName = GameInfo.GetStrings("en").itemlist[item]
            .ToLower()
            .Replace(" ", "")
            .Replace("é", "e");
        return $"https://serebii.net/itemdex/sprites/sv/{itemName}.png";
    }

    private static string GetMysteryEggTypeImageUrl(PKM pk)
    {
        var typeNames = new[]
        {
            "Normal", "Fighting", "Flying", "Poison", "Ground", "Rock", "Bug", "Ghost",
            "Steel", "Fire", "Water", "Grass", "Electric", "Psychic", "Ice", "Dragon",
            "Dark", "Fairy"
        };

        var typeIndex = pk.PersonalInfo.Type1;
        var typeName = typeIndex < typeNames.Length ? typeNames[typeIndex] : "Normal";
        return $"https://raw.githubusercontent.com/Daiivr/SysBot-Images/refs/heads/main/MysteryEggs/MEgg_{typeName}.png";
    }

    public static async Task<(int R, int G, int B)> GetDominantColorAsync(string imagePath)
    {
        try
        {
            using var image = await LoadImageAsync(imagePath);
            var colorCount = new Dictionary<Color, int>();

            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    if (pixel.A < 128 || pixel.GetBrightness() > 0.9)
                        continue;

                    var key = Color.FromArgb(
                        pixel.R / 10 * 10,
                        pixel.G / 10 * 10,
                        pixel.B / 10 * 10);

                    colorCount[key] = colorCount.TryGetValue(key, out var v) ? v + 1 : 1;
                }

            if (colorCount.Count == 0)
                return (255, 255, 255);

            var dom = colorCount.MaxBy(k => k.Value).Key;
            return (dom.R, dom.G, dom.B);
        }
        catch
        {
            return (255, 255, 255);
        }
    }

    private static async Task<Bitmap> LoadImageAsync(string imagePath)
    {
        if (!imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return new Bitmap(imagePath);

        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync(imagePath);
        return new Bitmap(stream);
    }
}
