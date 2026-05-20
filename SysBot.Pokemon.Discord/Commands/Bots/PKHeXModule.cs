using Discord.Commands;
using PKHeX.Core;
using SysBot.Pokemon.Localization;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class PKHeXModule<T> : SudoModule<T> where T : PKM, new()
{
    [Command("pkhex")]
    [Alias("pkh")]
    [Summary("Launch PKHeX on the bot host PC.")]
    [RequireOwner]
    public async Task LaunchPKHeXAsync()
    {
        try
        {
            var pkHeXDirectory = SysCord<T>.Runner.Config.Folder.PKHeXDirectory;

            if (string.IsNullOrWhiteSpace(pkHeXDirectory) || !Directory.Exists(pkHeXDirectory))
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordPkhexDirectoryMissing)).ConfigureAwait(false);
                return;
            }

            var exePath = Directory
                .GetFiles(pkHeXDirectory, "*.exe", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => Path.GetFileName(f).Contains("pkhex", StringComparison.OrdinalIgnoreCase));

            if (exePath == null)
            {
                await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordPkhexExecutableMissing)).ConfigureAwait(false);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = pkHeXDirectory,
                UseShellExecute = true
            });

            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordPkhexLaunched)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordPkhexLaunchFailed, ex.Message)).ConfigureAwait(false);
        }
    }
}
