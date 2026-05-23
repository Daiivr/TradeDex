using Discord;
using Discord.Commands;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private const string OriginalRepo = "https://github.com/kwsch/SysBot.NET";
        private const string PokeBotRepo = "https://github.com/hexbyt3/PokeBot";
        private const string TradeDexRepo = "https://github.com/Daiivr/TradeDex";
        private const string ProjectPokemon = "https://projectpokemon.org";
        private const string ThumbnailUrl = "https://i.imgur.com/jYp2WsN.png";

        [Command("info")]
        [Alias("about", "whoami", "owner", "bot")]
        [Summary("Shows general bot information, versions, and runtime environment.")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var me = Context.Client.CurrentUser;

            var builder = new EmbedBuilder()
                .WithAuthor(author =>
                {
                    author.Name = me?.Username ?? "TradeDex";
                    author.IconUrl = me?.GetAvatarUrl() ?? me?.GetDefaultAvatarUrl();
                })
                .WithTitle($"ℹ️ {AppLocalization.Get(LocalizationKeys.DiscordInfoTitle)}")
                .WithDescription(AppLocalization.Get(LocalizationKeys.DiscordInfoDescription))
                .WithColor(new Color(67, 181, 129))
                .WithThumbnailUrl(ThumbnailUrl)
                .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordRequestedByFooter, Context.User.Username), Context.User.GetAvatarUrl())
                .WithCurrentTimestamp();

            builder.AddField($"__**📦 {AppLocalization.Get(LocalizationKeys.DiscordInfoProjectTitle)}**__",
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoOriginalRepo))}: {Format.Url("SysBot.NET", OriginalRepo)}\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoForkedFrom))}: {Format.Url("PokeBot", PokeBotRepo)}\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoThisBot))}: {Format.Url("TradeDex", TradeDexRepo)}\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoOwner))}: {app.Owner} (`{app.Owner.Id}`)\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoLibrary))}: Discord.Net (`{DiscordConfig.Version}`)",
                inline: false);

            builder.AddField($"__**🏷️ {AppLocalization.Get(LocalizationKeys.DiscordInfoVersionsTitle)}**__",
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoFusionBotVersion))}: `{TradeBot.Version}`\n" +
                $"- {Format.Bold("PKHeX.Core")}: `{GetVersionInfo("PKHeX.Core")}`\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoAutoLegalityVersion))}: `{GetVersionInfo("PKHeX.Core.AutoMod")}`\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoBuildtime))}: `{GetVersionInfo("SysBot.Base", false)}`",
                inline: false);

            builder.AddField($"__**⚙️ {AppLocalization.Get(LocalizationKeys.DiscordInfoEnvironmentTitle)}**__",
                $"`{RuntimeInformation.FrameworkDescription}` `{RuntimeInformation.ProcessArchitecture}`\n" +
                $"`{RuntimeInformation.OSDescription}` `{RuntimeInformation.OSArchitecture}`\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoUptime))}: `{GetUptime()}`\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoHeapSize))}: `{GetHeapSize()} MiB`\n" +
                $"- {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoServers))}: `{Context.Client.Guilds.Count}` | " +
                $"{Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoChannels))}: `{Context.Client.Guilds.Sum(g => g.Channels.Count)}` | " +
                $"{Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoUsers))}: `{Context.Client.Guilds.Sum(g => g.MemberCount)}`",
                inline: false);

            builder.AddField($"__**🙏 {AppLocalization.Get(LocalizationKeys.DiscordInfoCreditsTitle)}**__",
                $"{AppLocalization.Get(LocalizationKeys.DiscordInfoCreditsBody)}\n" +
                $"{Format.Url("Project Pokémon", ProjectPokemon)}",
                inline: false);

            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordInfoReply), embed: builder.Build()).ConfigureAwait(false);
        }

        private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);

        private static string GetVersionInfo(string assemblyName, bool inclVersion = true)
        {
            var defaultValue = AppLocalization.Get(LocalizationKeys.DiscordUnknown);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = Array.Find(assemblies, x => x.GetName().Name == assemblyName);

            var attribute = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute is null)
                return defaultValue;

            var info = attribute.InformationalVersion;
            var split = info.Split('+');
            if (split.Length >= 2)
            {
                var version = split[0];
                var revision = split[1];
                if (DateTime.TryParseExact(revision, "yyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var buildTime))
                    return (inclVersion ? $"{version} " : "") + $@"{buildTime:yy-MM-dd\.hh\:mm}";
                return inclVersion ? version : defaultValue;
            }
            return defaultValue;
        }
    }
}
