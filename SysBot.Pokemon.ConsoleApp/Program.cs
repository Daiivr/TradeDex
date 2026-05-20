using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Discord.Helpers;
using SysBot.Pokemon.Localization;
using SysBot.Pokemon.Z3;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SysBot.Pokemon.ConsoleApp;

public static class Program
{
    private const string ConfigPath = "config.json";

    private static void ExitNoConfig()
    {
        var bot = new PokeBotState { Connection = new SwitchConnectionConfig { IP = "192.168.0.1", Port = 6000 }, InitialRoutine = PokeRoutineType.FlexTrade };
        var cfg = new ProgramConfig { Bots = [bot] };
        var created = JsonSerializer.Serialize(new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigPath, created);
        LogUtil.LogInfo("SysBot", L("A new configuration file was created because none was found in the program path. Please configure it and restart the program."));
        LogUtil.LogInfo("SysBot", L("It is recommended to configure this file using the GUI project if possible, as it will help you assign the values correctly."));
        LogUtil.LogInfo("SysBot", L("Press any key to exit."));
        Console.ReadKey();
    }

    private static void Main(string[] args)
    {
        _ = AppLocalization.Language;

        LogUtil.LogInfo("SysBot", L("Starting..."));
        if (args.Length > 1)
            LogUtil.LogInfo("SysBot", L("This program does not support command line arguments."));

        if (!File.Exists(ConfigPath))
        {
            ExitNoConfig();
            return;
        }

        try
        {
            var lines = File.ReadAllText(ConfigPath);
            var cfg = JsonSerializer.Deserialize<ProgramConfig>(lines) ?? new ProgramConfig();
            AppLocalization.SetLanguage(cfg.Language);
            AppLocalization.SetDiscordSettings(cfg.Hub.Discord);
            PokeTradeBotSWSH.SeedChecker = new Z3SeedSearchHandler<PK8>();
            BotContainer.RunBots(cfg);
        }
        catch (Exception)
        {
            LogUtil.LogInfo("SysBot", L("Could not start bots with the saved configuration file. Copy your configuration from the WinForms project or delete it and configure it again."));
            Console.ReadKey();
        }
    }

    private static string L(string message) => AppLocalization.LocalizeRuntimeMessage(message);
}

public static class BotContainer
{
    public static void RunBots(ProgramConfig prog)
    {
        // Establecer el modo de juego actual para BatchCommandNormalizer
        BatchCommandNormalizer.CurrentGameMode = prog.Mode;

        IPokeBotRunner env = GetRunner(prog);
        foreach (var bot in prog.Bots)
        {
            bot.Initialize();
            if (!AddBot(env, bot, prog.Mode))
                LogUtil.LogInfo("SysBot", L($"Could not add bot: {bot}"));
        }

        LogUtil.Forwarders.Add(ConsoleForwarder.Instance);
        env.StartAll();
        LogUtil.LogInfo("SysBot", L($"All bots started (Count: {prog.Bots.Length})."));
        LogUtil.LogInfo("SysBot", L("Press any key to stop running and exit. You can minimize this window if you want!"));
        Console.ReadKey();
        env.StopAll();
    }

    private static bool AddBot(IPokeBotRunner env, PokeBotState cfg, ProgramMode mode)
    {
        if (!cfg.IsValid())
        {
            LogUtil.LogInfo("SysBot", L($"The configuration for {cfg} is not valid."));
            return false;
        }

        PokeRoutineExecutorBase newBot;
        try
        {
            newBot = env.CreateBotFromConfig(cfg);
        }
        catch
        {
            LogUtil.LogInfo("SysBot", L($"The current mode ({mode}) does not support this bot type ({cfg.CurrentRoutineType})."));
            return false;
        }
        try
        {
            env.Add(newBot);
        }
        catch (ArgumentException ex)
        {
            LogUtil.LogInfo("SysBot", ex.Message);
            return false;
        }

        LogUtil.LogInfo("SysBot", L($"Added: {cfg}: {cfg.InitialRoutine}"));
        return true;
    }

    private static IPokeBotRunner GetRunner(ProgramConfig prog) => prog.Mode switch
    {
        ProgramMode.SWSH => new PokeBotRunnerImpl<PK8>(new PokeTradeHub<PK8>(prog.Hub), new BotFactory8SWSH(), prog),
        ProgramMode.BDSP => new PokeBotRunnerImpl<PB8>(new PokeTradeHub<PB8>(prog.Hub), new BotFactory8BS(), prog),
        ProgramMode.LA => new PokeBotRunnerImpl<PA8>(new PokeTradeHub<PA8>(prog.Hub), new BotFactory8LA(), prog),
        ProgramMode.SV => new PokeBotRunnerImpl<PK9>(new PokeTradeHub<PK9>(prog.Hub), new BotFactory9SV(), prog),
        ProgramMode.LGPE => new PokeBotRunnerImpl<PB7>(new PokeTradeHub<PB7>(prog.Hub), new BotFactory7LGPE(), prog),
        ProgramMode.PLZA => new PokeBotRunnerImpl<PA9>(new PokeTradeHub<PA9>(prog.Hub), new BotFactory9PLZA(), prog),
        _ => throw new IndexOutOfRangeException(L("Unsupported mode.")),
    };

    private static string L(string message) => AppLocalization.LocalizeRuntimeMessage(message);
}
