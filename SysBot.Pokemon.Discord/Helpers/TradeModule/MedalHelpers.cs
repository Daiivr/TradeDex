using Discord;
using Discord.WebSocket;
using SysBot.Pokemon.Localization;
using System.Linq;

namespace SysBot.Pokemon.Discord;

public static class MedalHelpers
{
    public static int GetCurrentMilestone(int totalTrades)
    {
        int[] milestones = [700, 650, 600, 550, 500, 450, 400, 350, 300, 250, 200, 150, 100, 50, 1];
        return milestones.FirstOrDefault(m => totalTrades >= m, 0);
    }

    public static Embed CreateMedalsEmbed(SocketUser user, int milestone, int totalTrades)
    {
        string status = milestone switch
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
            _ => AppLocalization.Get(LocalizationKeys.DiscordMedalStatusNewTrainer)
        };

        string description = AppLocalization.Format(LocalizationKeys.DiscordMedalDescription, totalTrades, status);

        if (milestone > 0)
        {
            string imageUrl = $"https://raw.githubusercontent.com/Secludedly/ZE-FusionBot-Sprite-Images/main/{milestone:D3}.png";
            return new EmbedBuilder()
                .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordTradingStatusTitle, user.Username))
                .WithColor(new Color(255, 215, 0))
                .WithDescription(description)
                .WithThumbnailUrl(imageUrl)
                .Build();
        }
        else
        {
            return new EmbedBuilder()
                .WithTitle(AppLocalization.Format(LocalizationKeys.DiscordTradingStatusTitle, user.Username))
                .WithColor(new Color(255, 215, 0))
                .WithDescription(description)
                .Build();
        }
    }
}
