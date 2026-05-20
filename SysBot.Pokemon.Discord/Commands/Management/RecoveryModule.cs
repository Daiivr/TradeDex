using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class RecoveryModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static IPokeBotRunner? Runner => SysCord<T>.Runner;

    [Command("recovery")]
    [Alias("recover")]
    [Summary("Shows the recovery status of all bots.")]
    [RequireSudo]
    public async Task ShowRecoveryStatusAsync()
    {
        if (Runner == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryRunnerNotInitialized)).ConfigureAwait(false);
            return;
        }

        if (Runner is not PokeBotRunner<T> runner)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryServiceUnavailable)).ConfigureAwait(false);
            return;
        }
        
        var recoveryService = runner.GetRecoveryService();
        
        if (recoveryService == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryServiceDisabled)).ConfigureAwait(false);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordRecoveryStatusTitle))
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.Now);

        var hasRecoveryData = false;
        foreach (var bot in Runner.Bots)
        {
            var state = bot.GetRecoveryState();
            if (state != null && (state.ConsecutiveFailures > 0 || state.CrashHistory.Count > 0))
            {
                hasRecoveryData = true;
                var status = bot.IsRunning
                    ? $"🟢 {AppLocalization.Get(LocalizationKeys.DiscordRecoveryBotRunning)}"
                    : $"🔴 {AppLocalization.Get(LocalizationKeys.DiscordRecoveryBotStopped)}";
                if (state.IsRecovering)
                    status = $"🟠 {AppLocalization.Get(LocalizationKeys.DiscordRecoveryBotRecovering)}";

                var fieldValue = AppLocalization.Format(LocalizationKeys.DiscordRecoveryStatusField, status, state.CrashHistory.Count, state.ConsecutiveFailures);
                
                if (state.LastRecoveryAttempt.HasValue)
                {
                    fieldValue += AppLocalization.Format(LocalizationKeys.DiscordRecoveryLastAttempt, state.LastRecoveryAttempt.Value);
                }
                
                embed.AddField(bot.Bot.Connection.Name, fieldValue, true);
            }
        }

        if (!hasRecoveryData)
        {
            embed.WithDescription(AppLocalization.Get(LocalizationKeys.DiscordRecoveryAllNormal));
        }

        await ReplyAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    [Command("recoveryReset")]
    [Alias("resetRecovery")]
    [Summary("Resets the recovery state for a specific bot.")]
    [RequireSudo]
    public async Task ResetRecoveryAsync([Remainder] string botName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botName);
        
        if (Runner == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryRunnerNotInitialized)).ConfigureAwait(false);
            return;
        }

        if (Runner is not PokeBotRunner<T> runner)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryServiceUnavailable)).ConfigureAwait(false);
            return;
        }
        
        var recoveryService = runner.GetRecoveryService();
        
        if (recoveryService == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryServiceDisabled)).ConfigureAwait(false);
            return;
        }

        var bot = Runner.Bots.FirstOrDefault(b => b.Bot.Connection.Name.Equals(botName, StringComparison.OrdinalIgnoreCase));
        if (bot == null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRecoveryBotNotFound, botName)).ConfigureAwait(false);
            return;
        }

        recoveryService.ResetRecoveryState(bot.Bot.Connection.Name);
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRecoveryResetDone, bot.Bot.Connection.Name)).ConfigureAwait(false);
    }

    [Command("recoveryToggle")]
    [Alias("toggleRecovery")]
    [Summary("Enables or disables the recovery system.")]
    [RequireSudo]
    public async Task ToggleRecoveryAsync()
    {
        if (Runner == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryRunnerNotInitialized)).ConfigureAwait(false);
            return;
        }

        if (Runner is not PokeBotRunner<T> runner)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryServiceUnavailable)).ConfigureAwait(false);
            return;
        }
        
        var config = Runner.Config.Recovery;
        config.EnableRecovery = !config.EnableRecovery;

        var status = config.EnableRecovery
            ? AppLocalization.Get(LocalizationKeys.DiscordEnabled)
            : AppLocalization.Get(LocalizationKeys.DiscordDisabled);
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRecoverySystemToggled, status)).ConfigureAwait(false);
        
        // Update the recovery service state
        if (config.EnableRecovery)
            runner.RecoveryService?.EnableRecovery();
        else
            runner.RecoveryService?.DisableRecovery();
    }

    [Command("recoveryConfig")]
    [Alias("recoveryCfg")]
    [Summary("Shows the current recovery configuration.")]
    [RequireSudo]
    public async Task ShowRecoveryConfigAsync()
    {
        if (Runner == null)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRecoveryRunnerNotInitialized)).ConfigureAwait(false);
            return;
        }

        var config = Runner.Config.Recovery;
        
        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigTitle))
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.Now)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigEnabled), AppLocalization.Get(config.EnableRecovery ? LocalizationKeys.DiscordYesStatus : LocalizationKeys.DiscordNoStatus), true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigMaxAttempts), config.MaxRecoveryAttempts, true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigInitialDelay), $"{config.InitialRecoveryDelaySeconds}s", true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigMaxDelay), $"{config.MaxRecoveryDelaySeconds}s", true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigBackoff), $"{config.BackoffMultiplier}x", true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigCrashWindow), $"{config.CrashHistoryWindowMinutes} min", true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigMaxCrashes), config.MaxCrashesInWindow, true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigRecoverIntentional), AppLocalization.Get(config.RecoverIntentionalStops ? LocalizationKeys.DiscordYesStatus : LocalizationKeys.DiscordNoStatus), true)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryConfigStableUptime), $"{config.MinimumStableUptimeSeconds}s", true);

        await ReplyAsync(embed: embed.Build()).ConfigureAwait(false);
    }
}
