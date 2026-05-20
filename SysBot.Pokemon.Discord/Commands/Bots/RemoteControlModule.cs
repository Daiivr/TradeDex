using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Remotely controls a bot.")]
public class RemoteControlModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("click")]
    [Summary("Clicks the specified button.")]
    [RequireRoleAccess(nameof(DiscordManager.RolesRemoteControl))]
    public async Task ClickAsync(SwitchButton b)
    {
        var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteNoBotCommand, b)).ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(b, bot).ConfigureAwait(false);
    }

    [Command("click")]
    [Summary("Clicks the specified button.")]
    [RequireSudo]
    public async Task ClickAsync(string ip, SwitchButton b)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteNoBotCommand, b)).ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(b, bot).ConfigureAwait(false);
    }

    [Command("setScreenOff")]
    [Alias("screenOff", "scrOff")]
    [Summary("Turns the screen off")]
    [RequireSudo]
    public async Task SetScreenOffAsync()
    {
        await SetScreen(false).ConfigureAwait(false);
    }

    [Command("setScreenOn")]
    [Alias("screenOn", "scrOn")]
    [Summary("Turns the screen on")]
    [RequireSudo]
    public async Task SetScreenOnAsync()
    {
        await SetScreen(true).ConfigureAwait(false);
    }

    [Command("screenOnAll")]
    [Alias("setScreenOnAll", "scrOnAll")]
    [Summary("Turns the screen on for all connected bots")]
    [RequireSudo]
    public async Task SetScreenOnAllAsync()
    {
        await SetScreenForAllBots(true).ConfigureAwait(false);
    }

    [Command("screenOffAll")]
    [Alias("setScreenOffAll", "scrOffAll")]
    [Summary("Turns the screen off for all connected bots")]
    [RequireSudo]
    public async Task SetScreenOffAllAsync()
    {
        await SetScreenForAllBots(false).ConfigureAwait(false);
    }

    private async Task SetScreenForAllBots(bool on)
    {
        var bots = SysCord<T>.Runner.Bots;
        if (bots.Count == 0)
        {
            await ReplyAsync(AppLocalization.Get(LocalizationKeys.DiscordRemoteNoBotsConnected)).ConfigureAwait(false);
            return;
        }

        int successCount = 0;
        foreach (var bot in bots)
        {
            try
            {
                var b = bot.Bot;
                var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
                await b.Connection.SendAsync(SwitchCommand.SetScreen(on ? ScreenState.On : ScreenState.Off, crlf), CancellationToken.None).ConfigureAwait(false);
                successCount++;
            }
            catch (Exception)
            {
                // Continue with other bots if one fails
            }
        }

        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteScreenAllSet, GetScreenState(on), successCount, bots.Count)).ConfigureAwait(false);
    }

    [Command("setStick")]
    [Summary("Sets the stick to the specified position.")]
    [RequireRoleAccess(nameof(DiscordManager.RolesRemoteControl))]
    public async Task SetStickAsync(SwitchStick s, short x, short y, ushort ms = 1_000)
    {
        var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteNoBotCommand, s)).ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
    }

    [Command("setStick")]
    [Summary("Sets the stick to the specified position.")]
    [RequireSudo]
    public async Task SetStickAsync(string ip, SwitchStick s, short x, short y, ushort ms = 1_000)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteNoBotIp, ip)).ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
    }

    private static BotSource<PokeBotState>? GetBot(string ip)
    {
        var r = SysCord<T>.Runner;
        return r.GetBot(ip) ?? r.Bots.Find(x => x.IsRunning); // safe fallback for users who mistype IP address for single bot instances
    }

    private static bool IsRemoteControlBot(RoutineExecutor<PokeBotState> botstate)
        => botstate is RemoteControlBotSWSH or RemoteControlBotBS or RemoteControlBotLA or RemoteControlBotSV;

    private async Task ClickAsyncImpl(SwitchButton button, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(typeof(SwitchButton), button))
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteUnknownButton, button)).ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.Click(button, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemotePerformed, b.Connection.Name, button)).ConfigureAwait(false);
    }

    private static string GetRunningBotIP()
    {
        var r = SysCord<T>.Runner;
        var runningBot = r.Bots.Find(x => x.IsRunning);

        // Check if a running bot is found
        if (runningBot != null)
        {
            return runningBot.Bot.Config.Connection.IP;
        }
        else
        {
            // Default IP address or logic if no running bot is found
            return "192.168.1.1";
        }
    }

    private async Task SetScreen(bool on)
    {
        string ip = RemoteControlModule<T>.GetRunningBotIP();
        var bot = GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteNoBotIp, ip)).ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.SetScreen(on ? ScreenState.On : ScreenState.Off, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteScreenSet, GetScreenState(on))).ConfigureAwait(false);
    }

    private async Task SetStickAsyncImpl(SwitchStick s, short x, short y, ushort ms, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(typeof(SwitchStick), s))
        {
            await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteUnknownStick, s)).ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.SetStick(s, x, y, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemotePerformed, b.Connection.Name, s)).ConfigureAwait(false);
        await Task.Delay(ms).ConfigureAwait(false);
        await b.Connection.SendAsync(SwitchCommand.ResetStick(s, crlf), CancellationToken.None).ConfigureAwait(false);
        await ReplyAsync(AppLocalization.Format(LocalizationKeys.DiscordRemoteStickReset, b.Connection.Name)).ConfigureAwait(false);
    }

    private static string GetScreenState(bool on) =>
        AppLocalization.Get(on ? LocalizationKeys.DiscordStateOn : LocalizationKeys.DiscordStateOff);
}
