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
        private const int MaxComponentTextLength = 3900;
        private static readonly Color InfoColor = new(67, 181, 129);

        [Command("info")]
        [Alias("about", "whoami", "owner", "bot")]
        [Summary("Shows general bot information, versions, and runtime environment.")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var me = Context.Client.CurrentUser;

            var component = BuildInfoComponent(app.Owner, me, Context.User);
            await Context.Channel.SendMessageAsync(components: component, flags: MessageFlags.ComponentsV2).ConfigureAwait(false);
        }

        private MessageComponent BuildInfoComponent(IUser owner, IUser? botUser, IUser requestingUser)
        {
            var botName = botUser?.Username ?? "TradeDex";
            var botAvatar = botUser?.GetAvatarUrl(size: 64) ?? botUser?.GetDefaultAvatarUrl() ?? ThumbnailUrl;
            var builder = new ComponentBuilderV2();
            var container = new ContainerBuilder()
                .WithAccentColor(InfoColor);

            var header = new SectionBuilder()
                .AddComponent(new TextDisplayBuilder(TrimComponentText(
                    $"**{botName}**\n" +
                    $"{AppLocalization.Get(LocalizationKeys.DiscordInfoReply)}\n\n" +
                    $"**ℹ️ {AppLocalization.Get(LocalizationKeys.DiscordInfoTitle)}**\n" +
                    $"{AppLocalization.Get(LocalizationKeys.DiscordInfoDescription)}")))
                .WithAccessory(new ThumbnailBuilder(
                    new UnfurledMediaItemProperties(botAvatar),
                    botName,
                    false));

            container.WithSection(header);

            AddInfoSection(container, $"📦 {AppLocalization.Get(LocalizationKeys.DiscordInfoProjectTitle)}",
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoOriginalRepo))}: {Format.Url("SysBot.NET", OriginalRepo)}\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoForkedFrom))}: {Format.Url("PokeBot", PokeBotRepo)}\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoThisBot))}: {Format.Url("TradeDex", TradeDexRepo)}\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoOwner))}: {owner} (`{owner.Id}`)\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoLibrary))}: Discord.Net (`{DiscordConfig.Version}`)");

            AddInfoSection(container, $"🏷️ {AppLocalization.Get(LocalizationKeys.DiscordInfoVersionsTitle)}",
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoFusionBotVersion))}: `{TradeBot.Version}`\n" +
                $"• {Format.Bold("PKHeX.Core")}: `{GetVersionInfo("PKHeX.Core")}`\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoAutoLegalityVersion))}: `{GetVersionInfo("PKHeX.Core.AutoMod")}`\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoBuildtime))}: `{GetVersionInfo("SysBot.Base", false)}`");

            AddInfoSection(container, $"⚙️ {AppLocalization.Get(LocalizationKeys.DiscordInfoEnvironmentTitle)}",
                $"`{RuntimeInformation.FrameworkDescription}` `{RuntimeInformation.ProcessArchitecture}`\n" +
                $"`{RuntimeInformation.OSDescription}` `{RuntimeInformation.OSArchitecture}`\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoUptime))}: `{GetUptime()}`\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoHeapSize))}: `{GetHeapSize()} MiB`\n" +
                $"• {Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoServers))}: `{Context.Client.Guilds.Count}` | " +
                $"{Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoChannels))}: `{Context.Client.Guilds.Sum(g => g.Channels.Count)}` | " +
                $"{Format.Bold(AppLocalization.Get(LocalizationKeys.DiscordInfoUsers))}: `{Context.Client.Guilds.Sum(g => g.MemberCount)}`");

            AddInfoSection(container, $"🙏 {AppLocalization.Get(LocalizationKeys.DiscordInfoCreditsTitle)}",
                $"{AppLocalization.Get(LocalizationKeys.DiscordInfoCreditsBody)}\n" +
                $"{Format.Url("Project Pokémon", ProjectPokemon)}");

            container.WithSeparator(SeparatorSpacingSize.Small, true);
            container.WithTextDisplay(TrimComponentText(
                $"{AppLocalization.Format(LocalizationKeys.DiscordRequestedByFooter, requestingUser.Username)} • <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:f>"));
            builder.WithContainer(container);
            return builder.Build();
        }

        private static void AddInfoSection(ContainerBuilder container, string title, string body)
        {
            container.WithSeparator(SeparatorSpacingSize.Small, true);
            container.WithTextDisplay(TrimComponentText($"**{title}**\n{body}"));
        }

        private static string TrimComponentText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "\u200B";

            return text.Length <= MaxComponentTextLength ? text : text[..(MaxComponentTextLength - 3)] + "...";
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
