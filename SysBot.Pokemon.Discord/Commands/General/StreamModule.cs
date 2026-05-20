using Discord;
using Discord.Commands;
using SysBot.Pokemon.Localization;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class StreamModule : ModuleBase<SocketCommandContext>
{
    private static readonly LocalizationKeys.StreamMessageKey[] StreamMessageKeys =
    [
        LocalizationKeys.StreamMessageKey.One,
        LocalizationKeys.StreamMessageKey.Two,
        LocalizationKeys.StreamMessageKey.Three,
        LocalizationKeys.StreamMessageKey.Four,
        LocalizationKeys.StreamMessageKey.Five,
    ];

    [Command("stream")]
    [Alias("streamlink")]
    [Summary("Returns the host stream link.")]
    public async Task StreamAsync()
    {
        var settings = SysCordSettings.Settings;
        var iconOption = settings.Stream.StreamIcon;
        var streamLink = string.IsNullOrWhiteSpace(settings.Stream.StreamLink)
            ? "https://twitch.tv/"
            : settings.Stream.StreamLink.Trim();

        var streamIconUrl = DiscordSettings.StreamOptions.StreamIconUrls[iconOption];
        var platformName = GetStreamPlatformName(iconOption);
        var streamMessage = AppLocalization.Get(GetStreamMessageKey());

        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordStreamTitle, platformName))
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordStreamDescription, streamMessage, streamLink))
            .WithUrl(streamLink)
            .WithThumbnailUrl(streamIconUrl)
            .WithColor(GetEmbedColor(iconOption))
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordStreamPlatformField), platformName, inline: true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordStreamLinkField), AppLocalization.Format(LocalizationKeys.DiscordStreamLinkValue, streamLink), inline: true)
            .WithImageUrl("https://i.imgur.com/OmLhdAS.gif")
            .WithFooter(footer =>
            {
                footer.Text = AppLocalization.Format(LocalizationKeys.DiscordRequestedBy, Context.User.Username);
                footer.IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl();
            })
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed).ConfigureAwait(false);
    }

    private static string GetStreamMessageKey() =>
        StreamMessageKeys[Random.Shared.Next(StreamMessageKeys.Length)] switch
        {
            LocalizationKeys.StreamMessageKey.One => LocalizationKeys.DiscordStreamMessageOne,
            LocalizationKeys.StreamMessageKey.Two => LocalizationKeys.DiscordStreamMessageTwo,
            LocalizationKeys.StreamMessageKey.Three => LocalizationKeys.DiscordStreamMessageThree,
            LocalizationKeys.StreamMessageKey.Four => LocalizationKeys.DiscordStreamMessageFour,
            _ => LocalizationKeys.DiscordStreamMessageFive,
        };

    private static Color GetEmbedColor(StreamIconOption icon) =>
        icon switch
        {
            StreamIconOption.Twitch => new Color(145, 70, 255),
            StreamIconOption.Youtube => new Color(255, 0, 0),
            StreamIconOption.Facebook => new Color(24, 119, 242),
            StreamIconOption.Kick => new Color(0, 255, 0),
            StreamIconOption.TikTok => new Color(0, 0, 0),
            _ => Color.Default
        };

    private static string GetStreamPlatformName(StreamIconOption icon) =>
        icon switch
        {
            StreamIconOption.Twitch => "Twitch",
            StreamIconOption.Youtube => "YouTube",
            StreamIconOption.Facebook => "Facebook",
            StreamIconOption.Kick => "Kick",
            StreamIconOption.TikTok => "TikTok",
            _ => "Stream"
        };
}
