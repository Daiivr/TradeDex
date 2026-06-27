using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Pokemon.Localization;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class LegalityCheckModule : ModuleBase<SocketCommandContext>
{
    private const string DefaultPokemonImageUrl = "https://i.imgur.com/MlkpDow.gif";
    private const int MaxComponentTextLength = 3900;

    [Command("lc"), Alias("check", "validate", "verify")]
    [Summary("Verifies the attachment for legality.")]
    public async Task LegalityCheck()
    {
        foreach (var att in (System.Collections.Generic.IReadOnlyCollection<Attachment>)Context.Message.Attachments)
            await LegalityCheck(att, false).ConfigureAwait(false);
    }

    [Command("lcv"), Alias("verbose")]
    [Summary("Verifies the attachment for legality with a verbose output.")]
    public async Task LegalityCheckVerbose()
    {
        foreach (var att in (System.Collections.Generic.IReadOnlyCollection<Attachment>)Context.Message.Attachments)
            await LegalityCheck(att, true).ConfigureAwait(false);
    }

    private async Task LegalityCheck(IAttachment att, bool verbose)
    {
        var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
        if (!download.Success)
        {
            await ReplyAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var pkm = download.Data!;
        var la = new LegalityAnalysis(pkm);
        var speciesName = GetSpeciesName(pkm);
        var status = la.Valid
            ? AppLocalization.Get(LocalizationKeys.DiscordLegalityValid)
            : AppLocalization.Get(LocalizationKeys.DiscordLegalityInvalid);
        var component = BuildLegalityComponent(
            pkm,
            speciesName,
            download.SanitizedFileName ?? AppLocalization.Get(LocalizationKeys.DiscordUnknown),
            status,
            la.Report(verbose),
            la.Valid ? Color.Green : Color.Red);

        await Context.Channel.SendMessageAsync(components: component, flags: MessageFlags.ComponentsV2).ConfigureAwait(false);
    }

    private static MessageComponent BuildLegalityComponent(PKM pkm, string speciesName, string fileName, string status, string report, Color color)
    {
        var builder = new ComponentBuilderV2();
        var container = new ContainerBuilder()
            .WithAccentColor(color);

        var header = new SectionBuilder()
            .AddComponent(new TextDisplayBuilder(TrimComponentText(
                $"**{AppLocalization.Get(LocalizationKeys.DiscordLegalityReply)}**\n" +
                $"**{speciesName}**\n" +
                AppLocalization.Format(LocalizationKeys.DiscordLegalityReportDescription, fileName))))
            .WithAccessory(new ThumbnailBuilder(
                new UnfurledMediaItemProperties(GetPokemonImageUrl(pkm)),
                speciesName,
                false));

        container.WithSection(header);
        container.WithSeparator(SeparatorSpacingSize.Small, true);
        container.WithTextDisplay(TrimComponentText($"**{status}**\n{report}"));

        builder.WithContainer(container);
        return builder.Build();
    }

    private static string GetSpeciesName(PKM pkm)
    {
        try
        {
            return GameInfo.Strings.Species[pkm.Species];
        }
        catch (Exception)
        {
            return AppLocalization.Get(LocalizationKeys.DiscordUnknown);
        }
    }

    private static string GetPokemonImageUrl(PKM pkm)
    {
        return pkm switch
        {
            PK8 pk8 => global::SysBot.Pokemon.Helpers.TradeExtensions<PK8>.PokeImg(pk8, pk8.CanGigantamax, false),
            PK9 pk9 => global::SysBot.Pokemon.Helpers.TradeExtensions<PK9>.PokeImg(pk9, false, false),
            PB8 pb8 => global::SysBot.Pokemon.Helpers.TradeExtensions<PB8>.PokeImg(pb8, false, false),
            PA8 pa8 => global::SysBot.Pokemon.Helpers.TradeExtensions<PB8>.PokeImg(pa8, false, false),
            PB7 pb7 => global::SysBot.Pokemon.Helpers.TradeExtensions<PB7>.PokeImg(pb7, false, false),
            _ => DefaultPokemonImageUrl,
        } ?? DefaultPokemonImageUrl;
    }

    private static string TrimComponentText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "\u200B";

        return text.Length <= MaxComponentTextLength ? text : text[..(MaxComponentTextLength - 3)] + "...";
    }
}
