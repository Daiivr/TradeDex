using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class AutoLegalityExtensionsDiscord
{
    private const string ErrorImageUrl = "https://i.imgur.com/Y64hLzW.gif";
    private const string ErrorThumbnailUrl = "https://i.imgur.com/DWLEXyu.png";
    private const string WarningIconUrl = "https://img.freepik.com/free-icon/warning_318-478601.jpg";
    private const string SuccessIconUrl = "https://i.imgur.com/U03TTRB.png";
    private const string DefaultPokemonImageUrl = "https://i.imgur.com/MlkpDow.gif";

    public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, ITrainerInfo sav, ShowdownSet set, Dictionary<string, bool>? userHTPreferences = null, byte requestedLanguage = 0)
    {
        if (set.Species <= 0)
        {
            await SendAutoLegalityEmbedAsync(channel, AppLocalization.Get(LocalizationKeys.DiscordUnableInterpretShowdown), Color.Orange, AppLocalization.Get(LocalizationKeys.DiscordLegalizationWarningTitle), includeErrorImage: true).ConfigureAwait(false);
            return;
        }

        try
        {
            // Check if this is an egg request based on nickname
            bool isEggRequest = set.Nickname.Equals("egg", StringComparison.CurrentCultureIgnoreCase)
                                && Breeding.CanHatchAsEgg(set.Species);

            PKM pkm;
            string result;
            IBattleTemplate? template = null;

            if (isEggRequest)
            {
                // Wrap the ShowdownSet directly in a RegenTemplate
                var regenTemplate = new RegenTemplate(set);

                // Generate egg using ALM
                pkm = sav.GenerateEgg(regenTemplate, out var eggResult);
                result = eggResult.ToString();
            }
            else
            {
                // Generate normally
                template = AutoLegalityWrapper.GetTemplate(set);
                pkm = sav.GetLegal(template, out result);
            }

            if (pkm == null)
            {
                await SendAutoLegalityEmbedAsync(channel, AppLocalization.Get(LocalizationKeys.DiscordFailedGenerateFromSet), Color.Red, AppLocalization.Get(LocalizationKeys.DiscordLegalizationErrorTitle), includeErrorImage: true).ConfigureAwait(false);
                return;
            }

            // Apply requested language now, before legality checks, so the analysis
            // sees the correct language. ALM only applies RegenTemplate language when
            // OT/TID/SID are also present; we must set it explicitly otherwise.
            if (requestedLanguage != 0)
                ApplyLanguageToSet(pkm, set, requestedLanguage);

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[set.Species];

            // If Z-A generation failed and we have a PA9, try every HOME-supported game
            // before giving up — mirrors the same fallback used by the trade path.
            if (!la.Valid && !isEggRequest && pkm is PA9 && template != null)
            {
                var fallback = TryGetAsHomePa9(template, spec);
                if (fallback != null)
                {
                    pkm = fallback;
                    if (requestedLanguage != 0)
                        ApplyLanguageToSet(pkm, set, requestedLanguage);
                    la = new LegalityAnalysis(pkm);
                }
            }

            if (!la.Valid)
            {
                var reason = result switch
                {
                    "Timeout" => AppLocalization.Format(LocalizationKeys.DiscordSpeciesSetTimeout, spec),
                    "VersionMismatch" => AppLocalization.Get(LocalizationKeys.DiscordVersionMismatch),
                    _ => AppLocalization.Format(LocalizationKeys.DiscordCreateSpeciesFailed, spec)
                };

                var imsg = AppLocalization.Format(LocalizationKeys.DiscordShowdownOopsReason, reason);
                if (result == "Failed" && !isEggRequest)
                    imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(set, sav, pkm)}";

                await SendAutoLegalityEmbedAsync(channel, imsg, Color.Red, AppLocalization.Get(LocalizationKeys.DiscordLegalizationErrorTitle), includeErrorImage: true).ConfigureAwait(false);
                return;
            }

            var msg = isEggRequest
                ? AppLocalization.Format(LocalizationKeys.DiscordLegalizedEgg, result, spec)
                : AppLocalization.Format(LocalizationKeys.DiscordLegalizedPkmShowdown, result, spec, la.EncounterOriginal.Name);
            await SendLegalizationSuccessEmbedAsync(channel, pkm, msg, GetRegenTemplateText(pkm), result, la.EncounterOriginal.Name).ConfigureAwait(false);
            await channel.SendPKMAsync(pkm).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(AutoLegalityExtensionsDiscord));
            var msg = AppLocalization.Format(LocalizationKeys.DiscordUnexpectedShowdownProblem, string.Join("\n", set.GetSetLines()), ex.Message);
            await SendAutoLegalityEmbedAsync(channel, msg, Color.Red, AppLocalization.Get(LocalizationKeys.DiscordLegalizationErrorTitle), includeErrorImage: true).ConfigureAwait(false);
        }
    }

    public static Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, string content, byte gen)
    {
        content = BatchCommandNormalizer.NormalizeBatchCommands(content);
        var userHTPreferences = ParseHyperTrainingCommandsPublic(content);
        content = ReusableActions.StripCodeBlock(content);
        byte requestedLanguage = ExtractAndStripLanguage(ref content);
        var set = new ShowdownSet(content);
        var sav = AutoLegalityWrapper.GetTrainerInfo(gen);
        return channel.ReplyWithLegalizedSetAsync(sav, set, userHTPreferences, requestedLanguage);
    }

    public static Task ReplyWithLegalizedSetAsync<T>(this ISocketMessageChannel channel, string content) where T : PKM, new()
    {
        content = BatchCommandNormalizer.NormalizeBatchCommands(content);
        var userHTPreferences = ParseHyperTrainingCommandsPublic(content);
        content = ReusableActions.StripCodeBlock(content);
        byte requestedLanguage = ExtractAndStripLanguage(ref content);
        var set = new ShowdownSet(content);
        var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
        return channel.ReplyWithLegalizedSetAsync(sav, set, userHTPreferences, requestedLanguage);
    }

    private static byte ExtractAndStripLanguage(ref string content)
    {
        byte lang = LanguageHelper.GetFinalLanguage(content, null, 0, _ => 0);
        if (lang == 0)
            return 0;
        var lines = content.Split('\n').Where(l => !l.TrimStart().StartsWith("Language:", StringComparison.OrdinalIgnoreCase));
        content = string.Join('\n', lines);
        return lang;
    }

    public static async Task ReplyWithLegalizedSetAsync(this ISocketMessageChannel channel, IAttachment att)
    {
        var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
        if (!download.Success)
        {
            var errorMessage = download.ErrorMessage ?? AppLocalization.Get(LocalizationKeys.BotStatusUnknown);
            await SendAutoLegalityEmbedAsync(channel, AppLocalization.Format(LocalizationKeys.DiscordDownloadPkmFailed, errorMessage), Color.Red, AppLocalization.Get(LocalizationKeys.DiscordLegalizationErrorTitle), includeErrorImage: true).ConfigureAwait(false);
            return;
        }

        var pkm = download.Data!;
        var sanitizedFileName = download.SanitizedFileName ?? AppLocalization.Get(LocalizationKeys.BotStatusUnknown);
        if (new LegalityAnalysis(pkm).Valid)
        {
            await SendAutoLegalityEmbedAsync(
                channel,
                AppLocalization.Format(LocalizationKeys.DiscordAlreadyLegalFile, sanitizedFileName),
                Color.Orange,
                AppLocalization.Get(LocalizationKeys.DiscordLegalizationWarningTitle),
                imageUrl: GetPokemonImageUrl(pkm),
                status: AppLocalization.Get(LocalizationKeys.DiscordLegalizationFailedStatus),
                reason: AppLocalization.Get(LocalizationKeys.DiscordAlreadyLegalFile).Replace("{0}: ", string.Empty, StringComparison.Ordinal))
                .ConfigureAwait(false);
            return;
        }

        var legal = pkm.LegalizePokemon();
        if (!new LegalityAnalysis(legal).Valid)
        {
            await SendAutoLegalityEmbedAsync(
                channel,
                AppLocalization.Format(LocalizationKeys.DiscordUnableLegalizeFile, sanitizedFileName),
                Color.Red,
                AppLocalization.Get(LocalizationKeys.DiscordLegalizationErrorTitle),
                includeErrorImage: true,
                imageUrl: GetPokemonImageUrl(pkm),
                status: AppLocalization.Get(LocalizationKeys.DiscordLegalizationFailedStatus))
                .ConfigureAwait(false);
            return;
        }

        legal.RefreshChecksum();

        await SendLegalizationSuccessEmbedAsync(channel, legal, AppLocalization.Format(LocalizationKeys.DiscordLegalizedPkmFile, sanitizedFileName, string.Empty).Trim(), GetRegenTemplateText(legal)).ConfigureAwait(false);
        await channel.SendPKMAsync(legal).ConfigureAwait(false);
    }

    private static async Task SendAutoLegalityEmbedAsync(ISocketMessageChannel channel, string description, Color color, string title, bool includeErrorImage = false, string? imageUrl = null, string? status = null, string? reason = null)
    {
        var embed = new EmbedBuilder()
            .WithColor(color)
            .WithAuthor(new EmbedAuthorBuilder
            {
                Name = title,
                IconUrl = color == Color.Green ? SuccessIconUrl : WarningIconUrl,
            })
            .WithDescription(description)
            .WithThumbnailUrl(ErrorThumbnailUrl);

        if (!string.IsNullOrWhiteSpace(status))
            embed.AddField(FormatFieldName(LocalizationKeys.DiscordLegalizationStatusLabel), status, true);
        if (!string.IsNullOrWhiteSpace(reason))
            embed.AddField(FormatFieldName(LocalizationKeys.DiscordLegalizationReasonLabel), reason, true);

        if (!string.IsNullOrWhiteSpace(imageUrl))
            embed.WithImageUrl(imageUrl);
        else if (includeErrorImage)
            embed.WithImageUrl(ErrorImageUrl);

        await channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private static async Task SendLegalizationSuccessEmbedAsync(ISocketMessageChannel channel, PKM pkm, string description, string showdownText, string? result = null, string? encounterName = null)
    {
        var species = GameInfo.Strings.Species[pkm.Species];
        var embed = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithAuthor(new EmbedAuthorBuilder
            {
                Name = AppLocalization.Get(LocalizationKeys.DiscordLegalizationSuccessTitle),
                IconUrl = SuccessIconUrl,
            })
            .WithDescription(description)
            .WithThumbnailUrl(GetPokemonImageUrl(pkm))
            .AddField(FormatFieldName(LocalizationKeys.DiscordLegalizationSpeciesLabel), species, true);

        if (!string.IsNullOrWhiteSpace(encounterName))
            embed.AddField(FormatFieldName(LocalizationKeys.DiscordLegalizationEncounterTypeLabel), GetEncounterTranslation(encounterName), true);
        if (!string.IsNullOrWhiteSpace(result))
            embed.AddField(FormatFieldName(LocalizationKeys.DiscordLegalizationResultLabel), GetLegalizationTranslation(result), true);

        embed.AddField(FormatFieldName(LocalizationKeys.DiscordLegalizationDetailsLabel), WrapCodeBlock(showdownText), inline: false)
            .WithFooter(AppLocalization.Get(LocalizationKeys.DiscordLegalizationCopyFooter));

        await channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    private static string FormatFieldName(string key) => $"__**{AppLocalization.Get(key)}**__";

    private static string GetLegalizationTranslation(string result)
    {
        if (AppLocalization.Language != AppLanguage.Spanish)
            return result;

        return result switch
        {
            "Regenerated" => "Regenerado",
            "Failed" => "Fallido",
            "Timeout" => "Tiempo de espera agotado",
            "VersionMismatch" => "Incompatibilidad de versiones",
            _ => result,
        };
    }

    private static string GetEncounterTranslation(string encounterName)
    {
        if (AppLocalization.Language != AppLanguage.Spanish)
            return encounterName;

        return encounterName switch
        {
            "Egg" => "Huevo",
            "Static Encounter" => "Encuentro estatico",
            "Wild Encounter (SV)" => "Encuentro salvaje (SV)",
            "Wild Encounter (PLA)" => "Encuentro salvaje (PLA)",
            "Wild Encounter (SH)" => "Encuentro salvaje (SH)",
            "Wild Encounter (SW)" => "Encuentro salvaje (SW)",
            "Wild Encounter (SP)" => "Encuentro salvaje (SP)",
            "Wild Encounter (BD)" => "Encuentro salvaje (BD)",
            _ => encounterName,
        };
    }

    private static string GetRegenTemplateText(PKM pkm)
    {
        var species = GameInfo.Strings.Species[pkm.Species];
        var regenText = new RegenTemplate(pkm).Text;
        var formNames = FormConverter.GetFormList(pkm.Species, GameInfo.Strings.Types, GameInfo.Strings.forms, new List<string>(), pkm.Context);
        var formName = pkm.Form > 0 && pkm.Form < formNames.Length ? formNames[pkm.Form] : string.Empty;
        var speciesForm = string.IsNullOrEmpty(formName) ? species : $"{species}-{formName}";

        return $"{speciesForm}\n{regenText}";
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

    private static string TrimEmbedField(string value, int maxLength = 1024)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..(maxLength - 3)] + "...";
    }

    private static string WrapCodeBlock(string value)
    {
        var trimmed = TrimEmbedField(value, 1016);
        return $"```{trimmed}```";
    }

    /// <summary>
    /// Checks if the normalized content contains hypertrain-related batch commands.
    /// Returns a dictionary of which stats were specified and their values.
    /// If null, no HT commands were specified.
    /// If dictionary contains "ALL" key with value 0, HyperTrainFlags=0 was specified (no HT at all).
    /// </summary>
    public static Dictionary<string, bool>? ParseHyperTrainingCommandsPublic(string content)
    {
        var htFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Check for .HyperTrainFlags=0 which means disable all hypertraining
        if (content.Contains(".HyperTrainFlags=0", StringComparison.OrdinalIgnoreCase))
        {
            htFlags["ALL"] = false;
            return htFlags;
        }

        // Check for individual HT flags
        if (content.Contains(".HT_HP=", StringComparison.OrdinalIgnoreCase))
        {
            htFlags["HP"] = !content.Contains(".HT_HP=False", StringComparison.OrdinalIgnoreCase);
        }
        if (content.Contains(".HT_ATK=", StringComparison.OrdinalIgnoreCase))
        {
            htFlags["ATK"] = !content.Contains(".HT_ATK=False", StringComparison.OrdinalIgnoreCase);
        }
        if (content.Contains(".HT_DEF=", StringComparison.OrdinalIgnoreCase))
        {
            htFlags["DEF"] = !content.Contains(".HT_DEF=False", StringComparison.OrdinalIgnoreCase);
        }
        if (content.Contains(".HT_SPA=", StringComparison.OrdinalIgnoreCase))
        {
            htFlags["SPA"] = !content.Contains(".HT_SPA=False", StringComparison.OrdinalIgnoreCase);
        }
        if (content.Contains(".HT_SPD=", StringComparison.OrdinalIgnoreCase))
        {
            htFlags["SPD"] = !content.Contains(".HT_SPD=False", StringComparison.OrdinalIgnoreCase);
        }
        if (content.Contains(".HT_SPE=", StringComparison.OrdinalIgnoreCase))
        {
            htFlags["SPE"] = !content.Contains(".HT_SPE=False", StringComparison.OrdinalIgnoreCase);
        }

        return htFlags.Count > 0 ? htFlags : null;
    }

    /// Sets language on a generated PKM, including the Asian OT truncation that
    /// PKHeX requires when the configured OT exceeds 6 characters (the limit
    /// PKHeX enforces for Japanese/Korean/Chinese Pokémon).
    private static void ApplyLanguageToSet(PKM pkm, ShowdownSet set, byte language)
    {
        pkm.Language = (int)language;

        bool isAsian = language is
            (byte)LanguageID.Japanese or
            (byte)LanguageID.Korean or
            (byte)LanguageID.ChineseS or
            (byte)LanguageID.ChineseT;

        if (isAsian && pkm.OriginalTrainerName.Length > 6)
        {
            const string shortOT = "王犬米";
            pkm.OriginalTrainerName = shortOT;
            // Simple property assignment leaves stale trash bytes from the previous
            // longer OT, which PKHeX's Trainer check flags as invalid. Clear them
            // explicitly (same approach used in the trade path's PrepareForTrade).
            var trashBuf = new byte[pkm.TrashCharCountTrainer * 2];
            int trashLen = pkm.SetString(trashBuf, shortOT.AsSpan(), pkm.TrashCharCountTrainer, StringConverterOption.ClearZero);
            pkm.OriginalTrainerTrash.Clear();
            trashBuf.AsSpan(0, trashLen).CopyTo(pkm.OriginalTrainerTrash);
        }

        if (string.IsNullOrEmpty(set.Nickname))
        {
            pkm.Nickname = SpeciesName.GetSpeciesNameGeneration(pkm.Species, pkm.Language, pkm.Format);
            pkm.IsNicknamed = false;
        }
        pkm.RefreshChecksum();
    }

    /// <summary>
    /// Mirrors the HOME fallback in Helpers&lt;T&gt;.TryGetAsHomePa9.
    /// Tries every PKM format HOME supports (newest first) and returns the first
    /// result that converts to a legally valid PA9.
    /// </summary>
    private static PA9? TryGetAsHomePa9(IBattleTemplate template, string speciesName)
    {
        (Func<ITrainerInfo> GetTrainer, string Name)[] sources =
        [
            (() => AutoLegalityWrapper.GetTrainerInfo<PK9>(),  "SV"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PK8>(),  "SWSH"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PA8>(),  "PLA"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PB8>(),  "BDSP"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PK7>(),  "USUM/SM"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PB7>(),  "LGPE"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PK6>(),  "ORAS/XY"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PK5>(),  "BW/B2W2"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PK4>(),  "DPPt/HGSS"),
            (() => AutoLegalityWrapper.GetTrainerInfo<PK3>(),  "RSE/FRLG"),
        ];

        foreach (var (getTrainer, name) in sources)
        {
            try
            {
                var trainerInfo = getTrainer();
                var generated = trainerInfo.GetLegal(template, out _);
                if (generated == null)
                    continue;

                var converted = EntityConverter.ConvertToType(generated, typeof(PA9), out _);
                if (converted is not PA9 pa9)
                    continue;

                if (!new LegalityAnalysis(pa9).Valid)
                    continue;

                LogUtil.LogInfo(
                    AppLocalization.Format(LocalizationKeys.LogHomeFallbackSucceeded, speciesName, name, pa9.Version),
                    "PA9HomeFallback");
                return pa9;
            }
            catch { }
        }

        return null;
    }
}
