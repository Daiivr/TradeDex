using Discord;
using Discord.Commands;
using SysBot.Pokemon.Localization;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class DonateModule : ModuleBase<SocketCommandContext>
{
    private static readonly LocalizationKeys.DonateMessageKey[] ThankYouMessageKeys =
    [
        LocalizationKeys.DonateMessageKey.One,
        LocalizationKeys.DonateMessageKey.Two,
        LocalizationKeys.DonateMessageKey.Three,
        LocalizationKeys.DonateMessageKey.Four,
        LocalizationKeys.DonateMessageKey.Five,
    ];

    [Command("donate")]
    [Alias("donation", "donar", "donación")]
    [Summary("Shows the host donation link, with progress bar if enabled.")]
    public async Task DonateAsync()
    {
        var donationSettings = SysCordSettings.Settings.Donation;
        var link = donationSettings.DonationLink?.Trim();
        if (string.IsNullOrWhiteSpace(link))
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordDonateNoLink)).ConfigureAwait(false);
            return;
        }

        var thankYouMessage = AppLocalization.Get(GetThankYouMessageKey());
        var embed = new EmbedBuilder();

        double goal = 0;
        double current = 0;
        double progress = 0;
        var showProgress = donationSettings.ProgressBar.ShowProgressBar;

        if (showProgress)
        {
            goal = ParseMoney(donationSettings.ProgressBar.DonationGoal);
            current = ParseMoney(donationSettings.ProgressBar.DonationCurrent);
            progress = goal > 0 ? Math.Clamp(current / goal, 0, 1) : 0;
            embed.WithColor(ProgressColor(progress));
        }
        else
        {
            embed.WithColor(new Color(255, 59, 48));
        }

        embed
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordDonateTitle))
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordDonateDescription, thankYouMessage, link))
            .WithUrl(link)
            .WithThumbnailUrl("https://i.imgur.com/0xwz3yL.png")
            .WithFooter(footer =>
            {
                footer.Text = AppLocalization.Format(LocalizationKeys.DiscordRequestedBy, Context.User.Username);
                footer.IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl();
            })
            .WithCurrentTimestamp();

        if (showProgress)
        {
            var bar = BuildProgressBar(progress, 12);
            var (fmtCurrent, fmtGoal, fmtRemaining) = FormatMoneyTriple(current, goal);

            embed.AddField(AppLocalization.Get(LocalizationKeys.DiscordDonateProgressField),
                AppLocalization.Format(LocalizationKeys.DiscordDonateProgressValue, bar, fmtCurrent, fmtGoal, progress * 100));

            if (goal > 0 && current < goal)
                embed.AddField(AppLocalization.Get(LocalizationKeys.DiscordDonateRemainingField), fmtRemaining, inline: true);
        }

        var components = new ComponentBuilder()
            .WithButton(AppLocalization.Get(LocalizationKeys.DiscordDonateButton), style: ButtonStyle.Link, url: link)
            .Build();

        await ReplyAsync(embed: embed.Build(), components: components).ConfigureAwait(false);
    }

    private static string GetThankYouMessageKey() =>
        ThankYouMessageKeys[Random.Shared.Next(ThankYouMessageKeys.Length)] switch
        {
            LocalizationKeys.DonateMessageKey.One => LocalizationKeys.DiscordDonateThanksOne,
            LocalizationKeys.DonateMessageKey.Two => LocalizationKeys.DiscordDonateThanksTwo,
            LocalizationKeys.DonateMessageKey.Three => LocalizationKeys.DiscordDonateThanksThree,
            LocalizationKeys.DonateMessageKey.Four => LocalizationKeys.DiscordDonateThanksFour,
            _ => LocalizationKeys.DiscordDonateThanksFive,
        };

    private static double ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var cleaned = Regex.Replace(value, @"[^\d.,-]", "").Trim();
        if (double.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var resultInv))
            return Math.Max(0, resultInv);

        var es = new CultureInfo("es-ES");
        if (double.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, es, out var resultEs))
            return Math.Max(0, resultEs);

        return 0;
    }

    private static (string fmtCurrent, string fmtGoal, string fmtRemaining) FormatMoneyTriple(double current, double goal)
    {
        static string F(double value) => value.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        var remaining = Math.Max(0, goal - current);
        return (F(current), F(goal), F(remaining));
    }

    private static string BuildProgressBar(double progress, int segments = 10)
    {
        progress = Math.Clamp(progress, 0, 1);
        segments = Math.Max(1, segments);

        const char filled = '▰';
        const char empty = '▱';

        var filledCount = (int)Math.Round(progress * segments, MidpointRounding.AwayFromZero);
        filledCount = Math.Clamp(filledCount, 0, segments);

        return new string(filled, filledCount) + new string(empty, segments - filledCount);
    }

    private static Color ProgressColor(double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        if (progress < 0.5)
        {
            var k = progress / 0.5;
            return new Color(Lerp(255, 255, k), Lerp(59, 204, k), Lerp(48, 0, k));
        }

        var secondHalf = (progress - 0.5) / 0.5;
        return new Color(Lerp(255, 16, secondHalf), Lerp(204, 185, secondHalf), Lerp(0, 129, secondHalf));
    }

    private static int Lerp(int a, int b, double t) =>
        a + (int)Math.Round((b - a) * Math.Clamp(t, 0, 1));
}
