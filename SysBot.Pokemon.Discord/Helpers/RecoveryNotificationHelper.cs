using Discord;
using Discord.WebSocket;
using SysBot.Base;
using SysBot.Pokemon.Localization;
using System;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

/// <summary>
/// Helper class for sending bot recovery notifications to Discord.
/// </summary>
public static class RecoveryNotificationHelper
{
    private static DiscordSocketClient? _client;
    private static ulong? _notificationChannelId;
    private static string _hubName = AppLocalization.Get(LocalizationKeys.DiscordRecoveryHubDefaultName);

    /// <summary>
    /// Initializes the recovery notification system with Discord client and channel.
    /// </summary>
    public static void Initialize(DiscordSocketClient client, ulong? notificationChannelId, string hubName)
    {
        _client = client;
        _notificationChannelId = notificationChannelId;
        _hubName = hubName;
    }

    /// <summary>
    /// Hooks up recovery events to Discord notifications.
    /// </summary>
    public static void HookRecoveryEvents<T>(BotRecoveryService<T> recoveryService) where T : class, IConsoleBotConfig
    {
        if (recoveryService == null || _client == null)
            return;

        recoveryService.BotCrashed += async (sender, e) => await OnBotCrashed(e);
        recoveryService.RecoveryAttempted += async (sender, e) => await OnRecoveryAttempted(e);
        recoveryService.RecoverySucceeded += async (sender, e) => await OnRecoverySucceeded(e);
        recoveryService.RecoveryFailed += async (sender, e) => await OnRecoveryFailed(e);
    }

    private static async Task OnBotCrashed(BotCrashEventArgs e)
    {
        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordRecoveryCrashDetectedTitle))
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordRecoveryCrashDescription, e.BotName, e.CrashTime))
            .WithColor(Color.Orange)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryStatusLabel), AppLocalization.Get(LocalizationKeys.DiscordRecoveryAttemptingAuto), false)
            .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordRecoveryFooter, _hubName))
            .Build();

        await SendNotificationAsync(embed);
    }

    private static async Task OnRecoveryAttempted(BotRecoveryEventArgs e)
    {
        if (!e.IsSuccess) // Only notify on attempts, not successes (handled separately)
        {
            var embed = new EmbedBuilder()
                .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordRecoveryAttemptTitle))
                .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordRecoveryAttemptDescription, e.BotName, e.AttemptNumber))
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordRecoveryFooter, _hubName))
                .Build();

            await SendNotificationAsync(embed);
        }
    }

    private static async Task OnRecoverySucceeded(BotRecoveryEventArgs e)
    {
        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordRecoverySuccessfulTitle))
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordRecoveryAttemptsDescription, e.BotName, e.AttemptNumber))
            .WithColor(Color.Green)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryStatusLabel), AppLocalization.Get(LocalizationKeys.DiscordRecoveryRunningNormally), false)
            .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordRecoveryFooter, _hubName))
            .Build();

        await SendNotificationAsync(embed);
    }

    private static async Task OnRecoveryFailed(BotRecoveryEventArgs e)
    {
        var embed = new EmbedBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordRecoveryFailedTitle))
            .WithDescription(AppLocalization.Format(LocalizationKeys.DiscordRecoveryAttemptsDescription, e.BotName, e.AttemptNumber))
            .WithColor(Color.Red)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryReasonLabel), e.FailureReason ?? AppLocalization.Get(LocalizationKeys.DiscordRecoveryUnknownError), false)
            .AddField(AppLocalization.Get(LocalizationKeys.DiscordRecoveryActionRequiredLabel), AppLocalization.Get(LocalizationKeys.DiscordRecoveryManualIntervention), false)
            .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordRecoveryFooter, _hubName))
            .Build();

        await SendNotificationAsync(embed);
    }

    private static async Task SendNotificationAsync(Embed embed)
    {
        try
        {
            if (_client == null || !_notificationChannelId.HasValue)
                return;

            if (_client.GetChannel(_notificationChannelId.Value) is ISocketMessageChannel channel)
            {
                await channel.SendMessageAsync(embed: embed);
            }
        }
        catch (Exception ex)
        {
            LogUtil.LogError(AppLocalization.Format(LocalizationKeys.LogRecoveryNotificationFailed, ex.Message), "RecoveryNotification");
        }
    }

    /// <summary>
    /// Sends a custom recovery notification.
    /// </summary>
    public static async Task SendCustomNotificationAsync(string title, string description, Color color)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordRecoveryFooter, _hubName))
            .Build();

        await SendNotificationAsync(embed);
    }

    /// <summary>
    /// Sends a recovery summary report.
    /// </summary>
    public static async Task SendRecoverySummaryAsync<T>(BotRunner<T> runner, BotRecoveryService<T> recoveryService) 
        where T : class, IConsoleBotConfig
    {
        var embedBuilder = new EmbedBuilder()
            .WithTitle(AppLocalization.Get(LocalizationKeys.DiscordRecoverySummaryTitle))
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithFooter(AppLocalization.Format(LocalizationKeys.DiscordRecoveryFooter, _hubName));

        foreach (var bot in runner.Bots)
        {
            var state = bot.GetRecoveryState();
            if (state != null && (state.ConsecutiveFailures > 0 || state.CrashHistory.Count > 0))
            {
                var status = bot.IsRunning
                    ? $"🟢 {AppLocalization.Get(LocalizationKeys.DiscordRecoveryBotRunning)}"
                    : $"🔴 {AppLocalization.Get(LocalizationKeys.DiscordRecoveryBotStopped)}";
                var fieldValue = AppLocalization.Format(LocalizationKeys.DiscordRecoveryStatusField, status, state.CrashHistory.Count, state.ConsecutiveFailures);
                
                embedBuilder.AddField(bot.Bot.Connection.Name, fieldValue, true);
            }
        }

        if (embedBuilder.Fields.Count == 0)
        {
            embedBuilder.WithDescription(AppLocalization.Get(LocalizationKeys.DiscordRecoveryAllNormal));
        }

        await SendNotificationAsync(embedBuilder.Build());
    }
}
