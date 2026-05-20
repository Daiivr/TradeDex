using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

// ReSharper disable once UnusedType.Global
public class BatchEditingModule : ModuleBase<SocketCommandContext>
{
    [Command("batchInfo"), Alias("bei")]
    [Summary("Tries to get info about the requested property.")]
    public async Task GetBatchInfo(string propertyName)
    {
        if (TryGetPropertyInfo(propertyName, out string? result))
        {
            result ??= AppLocalization.Get(LocalizationKeys.BotStatusUnknown);
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordBatchInfoResult, propertyName, result)).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordBatchInfoNotFound, propertyName)).ConfigureAwait(false);
        }
    }

    [Command("batchValidate"), Alias("bev")]
    [Summary("Tries to get info about the requested property.")]
    public async Task ValidateBatchInfo(string instructions)
    {
        bool valid = IsValidInstructionSet(instructions, out var invalid);

        if (!valid)
        {
            var msg = invalid.Select(z => $"{z.PropertyName}, {z.PropertyValue}");
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordBatchInvalidLines, Format.Code(string.Join(Environment.NewLine, msg))))
                .ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordBatchInvalidLineCount, invalid.Count)).ConfigureAwait(false);
        }
    }

    private static bool IsValidInstructionSet(ReadOnlySpan<char> split, out List<StringInstruction> invalid)
    {
        invalid = [];
        var set = new StringInstructionSet(split);
        foreach (var s in set.Filters.Concat(set.Instructions))
        {
            if (!TryGetPropertyInfo(s.PropertyName, out string? _))
                invalid.Add(s);
        }
        return invalid.Count == 0;
    }

    private static bool TryGetPropertyInfo(string propertyName, out string? result)
    {
        result = null;
        try
        {
            // Use reflection to check if property exists on common PKM types
            var pk = new PA9(); // Use Z-A as the most recent generation
            var prop = pk.GetType().GetProperty(propertyName);

            if (prop != null)
            {
                result = prop.PropertyType.Name;
                return true;
            }

            // If not found on PA9, try PKM base type
            prop = typeof(PKM).GetProperty(propertyName);
            if (prop != null)
            {
                result = prop.PropertyType.Name;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
