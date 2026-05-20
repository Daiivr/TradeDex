using Discord;
using Discord.Commands;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class BatchHelpers<T> where T : PKM, new()
{
    public static List<string> ParseBatchTradeContent(string content)
    {
        var delimiters = new[] { "---", "—-" };
        return [.. content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Select(trade => trade.Trim())];
    }

    public static async Task<(T? Pokemon, string? Error, ShowdownSet? Set, string? LegalizationHint)>
        ProcessSingleTradeForBatch(string tradeContent)
    {
        tradeContent = ReusableActions.StripCodeBlock(tradeContent);
        tradeContent = BatchCommandNormalizer.NormalizeBatchCommands(tradeContent);

        // Parse hypertraining preferences before processing
        var userHTPreferences = AutoLegalityExtensionsDiscord.ParseHyperTrainingCommandsPublic(tradeContent);

        var result = await Helpers<T>.ProcessShowdownSetAsync(tradeContent);

        if (result.Pokemon != null)
        {
            var pk = result.Pokemon;
            var set = result.ShowdownSet;

            return (pk, null, result.ShowdownSet, null);
        }

        return (null, result.Error, result.ShowdownSet, result.LegalizationHint);
    }

    public static async Task SendBatchErrorEmbedAsync(SocketCommandContext context, List<BatchTradeError> errors, int totalTrades)
    {
        errors ??= [];
        var failed = errors.Count;
        var succeeded = Math.Max(0, totalTrades - failed);
        var formattedTime = DateTime.UtcNow.ToString("hh:mm tt");

        var embed = new EmbedBuilder()
            .WithTitle("⚠️ " + AppLocalization.Get(LocalizationKeys.DiscordBatchValidationFailedTitle))
            .WithColor(Color.Red)
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordBatchValidationFailedSummaryDescription, totalTrades, succeeded, failed))
            .WithImageUrl("https://i.imgur.com/Y64hLzW.gif")
            .WithThumbnailUrl("https://i.imgur.com/DWLEXyu.png")
            .WithAuthor(AppLocalization.Get(LocalizationKeys.DiscordErrorLabel), "https://img.freepik.com/free-icon/warning_318-478601.jpg")
            .WithFooter(f =>
            {
                f.Text = $"{context.User.Username} • {formattedTime}";
                f.IconUrl = context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl();
            });

        const int maxFields = 10;
        foreach (var error in errors.OrderBy(x => x.TradeNumber).Take(maxFields))
        {
            var species = string.IsNullOrWhiteSpace(error.SpeciesName)
                ? AppLocalization.Get(LocalizationKeys.DiscordUnknownSpecies)
                : error.SpeciesName;

            var fieldValue = $"__**{AppLocalization.Get(LocalizationKeys.DiscordErrorLabel)}**__: {error.ErrorMessage}".Trim();
            if (!string.IsNullOrWhiteSpace(error.LegalizationHint))
            {
                var hint = error.LegalizationHint.Trim();
                if (hint.Length > 700)
                    hint = hint[..700] + "...";

                fieldValue += $"\n__{AppLocalization.Get(LocalizationKeys.DiscordHintLabel)}__: {hint}";
            }

            if (!string.IsNullOrWhiteSpace(error.ShowdownSet))
            {
                var lines = error.ShowdownSet.Split('\n');
                var preview = string.Join(" | ", lines.Take(2).Select(s => s.Trim()));
                if (preview.Length > 200)
                    preview = preview[..200] + "...";

                fieldValue += $"\n__{AppLocalization.Get(LocalizationKeys.DiscordSetLabel)}__: {preview}";
            }

            if (fieldValue.Length > 1024)
                fieldValue = fieldValue[..1021] + "...";

            var fieldName = AppLocalization.Format(LocalizationKeys.DiscordBatchTradeField, error.TradeNumber, species);
            if (fieldName.Length > 256)
                fieldName = fieldName[..253] + "...";

            embed.AddField(fieldName, fieldValue, inline: false);
        }

        if (failed > maxFields)
        {
            embed.AddField(
                AppLocalization.Get(LocalizationKeys.DiscordBatchAdditionalErrorsField),
                AppLocalization.Format(LocalizationKeys.DiscordBatchAdditionalErrorsValue, failed - maxFields),
                inline: false);
        }

        var replyMessage = await context.Channel
            .SendMessageAsync(text: context.User.Mention, embed: embed.Build())
            .ConfigureAwait(false);
        _ = Helpers<T>.DeleteMessagesAfterDelayAsync(replyMessage, context.Message, 30);
    }

    public static async Task ProcessBatchContainer(SocketCommandContext context, List<T> batchPokemonList,
        int batchTradeCode, int totalTrades)
    {
        var sig = context.User.GetFavor();
        var firstPokemon = batchPokemonList[0];

        await QueueHelper<T>.AddBatchContainerToQueueAsync(context, batchTradeCode, context.User.Username,
            firstPokemon, batchPokemonList, sig, context.User, totalTrades).ConfigureAwait(false);
    }

    public static string BuildDetailedBatchErrorMessage(List<BatchTradeError> errors, int totalTrades)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"**{AppLocalization.Get(LocalizationKeys.DiscordBatchValidationFailedTitle)}**");
        sb.AppendLine($"❌ {AppLocalization.Format(LocalizationKeys.DiscordBatchValidationFailedDescription, errors.Count, totalTrades)}\n");

        foreach (var error in errors)
        {
            sb.AppendLine($"**{AppLocalization.Format(LocalizationKeys.DiscordBatchTradeField, error.TradeNumber, error.SpeciesName)}**");
            sb.AppendLine($"{AppLocalization.Get(LocalizationKeys.DiscordErrorLabel)}: {error.ErrorMessage}");

            if (!string.IsNullOrEmpty(error.LegalizationHint))
            {
                sb.AppendLine($"💡 {AppLocalization.Get(LocalizationKeys.DiscordHintLabel)}: {error.LegalizationHint}");
            }

            if (!string.IsNullOrEmpty(error.ShowdownSet))
            {
                var lines = error.ShowdownSet.Split('\n').Take(3);
                sb.AppendLine($"{AppLocalization.Get(LocalizationKeys.DiscordSetLabel)}: {string.Join(" | ", lines)}...");
            }

            sb.AppendLine();
        }

        sb.AppendLine($"**{AppLocalization.Get(LocalizationKeys.DiscordBatchValidationFailedFooter)}**");
        return sb.ToString();
    }
}
