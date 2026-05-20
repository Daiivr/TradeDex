using System;
using System.Collections.Generic;
using System.Globalization;
using SysBot.Base;

namespace SysBot.Pokemon.Localization;

public static partial class AppLocalization
{
    private static readonly object Sync = new();

    private static AppLanguage _language = AppLanguage.English;
    private static DiscordSettings.MessageIconSettings? _discordMessageIcons;
    private static Func<DiscordSettings.MessageIconSettings?>? _discordMessageIconsProvider;

    static AppLocalization()
    {
        LogUtil.MessageLocalizer = LocalizeLogMessage;
    }

    public static AppLanguage Language
    {
        get
        {
            lock (Sync)
                return _language;
        }
    }

    public static CultureInfo Culture => Language switch
    {
        AppLanguage.Spanish => CultureInfo.GetCultureInfo("es"),
        _ => CultureInfo.GetCultureInfo("en"),
    };

    public static event EventHandler? LanguageChanged;

    public static void SetDiscordMessageIcons(DiscordSettings.MessageIconSettings? icons)
    {
        lock (Sync)
        {
            _discordMessageIcons = icons;
            _discordMessageIconsProvider = null;
        }
    }

    public static void SetDiscordSettings(DiscordSettings? settings)
    {
        lock (Sync)
        {
            _discordMessageIcons = settings?.MessageIcons;
            _discordMessageIconsProvider = settings is null ? null : () => settings.MessageIcons;
        }
    }

    public static void SetLanguage(AppLanguage language)
    {
        EventHandler? changed = null;

        lock (Sync)
        {
            var isChanged = _language != language;
            _language = language;
            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = Culture;

            if (isChanged)
                changed = LanguageChanged;
        }

        changed?.Invoke(null, EventArgs.Empty);
    }

    public static string Get(string key) => Get(key, Language);

    public static string Format(string key, params object[] args) =>
        string.Format(Culture, Get(key), args);

    public static string GetCommandSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return Get(LocalizationKeys.DiscordHelpNoDescription);

        if (CommandSummaryTranslations.TryGetValue(Language, out var localized) &&
            localized.TryGetValue(summary, out var value))
        {
            return value;
        }

        var runtime = LocalizeRuntimeMessage(summary);
        return runtime == summary ? summary : runtime;
    }

    public static string LocalizeLogMessage(string message)
    {
        return LocalizeRuntimeMessage(message);
    }

    public static string LocalizeRuntimeMessage(string message)
    {
        if (Language != AppLanguage.Spanish || string.IsNullOrWhiteSpace(message))
            return LocalizeRuntimeMessage(message, AppLanguage.English);

        return LocalizeRuntimeMessage(message, AppLanguage.Spanish);
    }

    private static string LocalizeRuntimeMessage(string message, AppLanguage language)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        var exactTranslations = language is AppLanguage.Spanish
            ? LogMessageTranslations
            : ReverseLogMessageTranslations;

        if (exactTranslations.TryGetValue(message, out var exact))
            return ApplyRuntimeFragments(exact, language);

        var runtimeExactTranslations = language is AppLanguage.Spanish
            ? RuntimeMessageTranslationsToSpanish
            : RuntimeMessageTranslationsToEnglish;

        if (runtimeExactTranslations.TryGetValue(message, out exact))
            return ApplyRuntimeFragments(exact, language);

        var prefixTranslations = language is AppLanguage.Spanish
            ? LogPrefixTranslations
            : ReverseLogPrefixTranslations;

        foreach (var (prefix, replacement) in prefixTranslations)
        {
            if (message.StartsWith(prefix, StringComparison.Ordinal))
                return ApplyRuntimeFragments(replacement + message[prefix.Length..], language);
        }

        var runtimePrefixTranslations = language is AppLanguage.Spanish
            ? RuntimePrefixTranslationsToSpanish
            : RuntimePrefixTranslationsToEnglish;

        foreach (var (prefix, replacement) in runtimePrefixTranslations)
        {
            if (message.StartsWith(prefix, StringComparison.Ordinal))
                return ApplyRuntimeFragments(replacement + message[prefix.Length..], language);
        }

        return ApplyRuntimeFragments(ApplyRuntimeFallback(message, language), language);
    }

    private static string ApplyRuntimeFallback(string message, AppLanguage language)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        var original = message;
        var phraseFallbacks = language is AppLanguage.Spanish
            ? RuntimeEnglishToSpanishPhraseFallbacks
            : RuntimeSpanishToEnglishPhraseFallbacks;

        foreach (var (source, replacement) in phraseFallbacks)
            message = message.Replace(source, replacement, StringComparison.OrdinalIgnoreCase);

        if (!string.Equals(message, original, StringComparison.Ordinal))
            return message;

        var wordFallbacks = language is AppLanguage.Spanish
            ? RuntimeEnglishToSpanishWordFallbacks
            : RuntimeSpanishToEnglishWordFallbacks;

        return TranslateRuntimeWords(message, wordFallbacks);
    }

    private static string TranslateRuntimeWords(string message, IReadOnlyDictionary<string, string> wordFallbacks)
    {
        var parts = message.Split(' ');
        for (var i = 0; i < parts.Length; i++)
        {
            var token = parts[i];
            var leading = string.Empty;
            var trailing = string.Empty;

            while (token.Length > 0 && char.IsPunctuation(token[0]) && token[0] is not '@' and not '#' and not '$')
            {
                leading += token[0];
                token = token[1..];
            }

            while (token.Length > 0 && char.IsPunctuation(token[^1]) && token[^1] is not ')' and not ']')
            {
                trailing = token[^1] + trailing;
                token = token[..^1];
            }

            if (wordFallbacks.TryGetValue(token, out var translated))
                parts[i] = leading + translated + trailing;
        }

        return string.Join(' ', parts);
    }

    private static string ApplyRuntimeFragments(string message, AppLanguage language)
    {
        var fragmentTranslations = language is AppLanguage.Spanish
            ? RuntimeFragmentTranslationsToSpanish
            : RuntimeSpanishFragmentTranslationsToEnglish;

        foreach (var (source, replacement) in fragmentTranslations)
            message = message.Replace(source, replacement, StringComparison.Ordinal);

        return ApplyDiscordMessageIcons(message);
    }

    private static string Get(string key, AppLanguage language)
    {
        if (Translations.TryGetValue(language, out var localized) &&
            localized.TryGetValue(key, out var value))
        {
            return ApplyDiscordMessageIcons(value);
        }

        if (Translations[AppLanguage.English].TryGetValue(key, out var fallback))
            return ApplyDiscordMessageIcons(fallback);

        return key;
    }

    private static string ApplyDiscordMessageIcons(string value)
    {
        DiscordSettings.MessageIconSettings? icons;
        lock (Sync)
            icons = _discordMessageIconsProvider?.Invoke() ?? _discordMessageIcons;

        if (icons is null || string.IsNullOrEmpty(value))
            return value;

        const string warningToken = "\uE000";
        const string successToken = "\uE001";
        const string errorToken = "\uE002";
        const string directMessageToken = "\uE003";
        const string waitingToken = "\uE004";

        return value
            .Replace(DiscordSettings.MessageIconSettings.DefaultWarning, warningToken, StringComparison.Ordinal)
            .Replace(DiscordSettings.MessageIconSettings.DefaultSuccess, successToken, StringComparison.Ordinal)
            .Replace(DiscordSettings.MessageIconSettings.DefaultError, errorToken, StringComparison.Ordinal)
            .Replace(DiscordSettings.MessageIconSettings.DefaultDirectMessage, directMessageToken, StringComparison.Ordinal)
            .Replace(DiscordSettings.MessageIconSettings.DefaultWaiting, waitingToken, StringComparison.Ordinal)
            .Replace(warningToken, icons.WarningOrDefault, StringComparison.Ordinal)
            .Replace(successToken, icons.SuccessOrDefault, StringComparison.Ordinal)
            .Replace(errorToken, icons.ErrorOrDefault, StringComparison.Ordinal)
            .Replace(directMessageToken, icons.DirectMessageOrDefault, StringComparison.Ordinal)
            .Replace(waitingToken, icons.WaitingOrDefault, StringComparison.Ordinal);
    }

    private static readonly IReadOnlyDictionary<AppLanguage, IReadOnlyDictionary<string, string>> Translations = BuildTranslations();
    private static IReadOnlyDictionary<AppLanguage, IReadOnlyDictionary<string, string>> BuildTranslations()
    {
        var english = new Dictionary<string, string>();
        var spanish = new Dictionary<string, string>();
        AddHudMainWindowAndSharedDialogsTranslations(english, AppLanguage.English);
        AddHudMainWindowAndSharedDialogsTranslations(spanish, AppLanguage.Spanish);
        AddHudBotsPageTranslations(english, AppLanguage.English);
        AddHudBotsPageTranslations(spanish, AppLanguage.Spanish);
        AddHudLogsPageTranslations(english, AppLanguage.English);
        AddHudLogsPageTranslations(spanish, AppLanguage.Spanish);
        AddHudBotControllerCardsAndContextMenuTranslations(english, AppLanguage.English);
        AddHudBotControllerCardsAndContextMenuTranslations(spanish, AppLanguage.Spanish);
        AddSharedLabelsLogsAndAlertsTranslations(english, AppLanguage.English);
        AddSharedLabelsLogsAndAlertsTranslations(spanish, AppLanguage.Spanish);
        AddDiscordQueueModuleTranslations(english, AppLanguage.English);
        AddDiscordQueueModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordTradeNotifierTranslations(english, AppLanguage.English);
        AddDiscordTradeNotifierTranslations(spanish, AppLanguage.Spanish);
        AddDiscordCloneModuleAndDumpModuleTranslations(english, AppLanguage.English);
        AddDiscordCloneModuleAndDumpModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordMysteryEggModuleTranslations(english, AppLanguage.English);
        AddDiscordMysteryEggModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordTradeModuleCommandsTranslations(english, AppLanguage.English);
        AddDiscordTradeModuleCommandsTranslations(spanish, AppLanguage.Spanish);
        AddDiscordTradeModuleBatchHelpersTranslations(english, AppLanguage.English);
        AddDiscordTradeModuleBatchHelpersTranslations(spanish, AppLanguage.Spanish);
        AddDiscordTradeModuleHelpersTranslations(english, AppLanguage.English);
        AddDiscordTradeModuleHelpersTranslations(spanish, AppLanguage.Spanish);
        AddDiscordTradeModuleListHelpersTranslations(english, AppLanguage.English);
        AddDiscordTradeModuleListHelpersTranslations(spanish, AppLanguage.Spanish);
        AddDiscordEmbedHelperTranslations(english, AppLanguage.English);
        AddDiscordEmbedHelperTranslations(spanish, AppLanguage.Spanish);
        AddDiscordQueueHelperTranslations(english, AppLanguage.English);
        AddDiscordQueueHelperTranslations(spanish, AppLanguage.Spanish);
        AddDiscordQueueHelperMilestonesAndErrorsTranslations(english, AppLanguage.English);
        AddDiscordQueueHelperMilestonesAndErrorsTranslations(spanish, AppLanguage.Spanish);
        AddDiscordHOMEReadyModuleTranslations(english, AppLanguage.English);
        AddDiscordHOMEReadyModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordPKHeXModuleTranslations(english, AppLanguage.English);
        AddDiscordPKHeXModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordRemoteControlModuleTranslations(english, AppLanguage.English);
        AddDiscordRemoteControlModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordSlashCommandsTranslations(english, AppLanguage.English);
        AddDiscordSlashCommandsTranslations(spanish, AppLanguage.Spanish);
        AddDiscordHelpModuleTranslations(english, AppLanguage.English);
        AddDiscordHelpModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordStreamAndDonateModuleTranslations(english, AppLanguage.English);
        AddDiscordStreamAndDonateModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordMysteryMonModuleTranslations(english, AppLanguage.English);
        AddDiscordMysteryMonModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordAutoLegalityExtensionsDiscordTranslations(english, AppLanguage.English);
        AddDiscordAutoLegalityExtensionsDiscordTranslations(spanish, AppLanguage.Spanish);
        AddDiscordReusableActionsTranslations(english, AppLanguage.English);
        AddDiscordReusableActionsTranslations(spanish, AppLanguage.Spanish);
        AddDiscordBatchEditingModuleTranslations(english, AppLanguage.English);
        AddDiscordBatchEditingModuleTranslations(spanish, AppLanguage.Spanish);
        AddDiscordManagementModulesTranslations(english, AppLanguage.English);
        AddDiscordManagementModulesTranslations(spanish, AppLanguage.Spanish);
        AddDiscordGeneralAndExtraModulesTranslations(english, AppLanguage.English);
        AddDiscordGeneralAndExtraModulesTranslations(spanish, AppLanguage.Spanish);
        AddLogsMysteryMonModuleTranslations(english, AppLanguage.English);
        AddLogsMysteryMonModuleTranslations(spanish, AppLanguage.Spanish);
        AddLogsTradeModuleHelpersTranslations(english, AppLanguage.English);
        AddLogsTradeModuleHelpersTranslations(spanish, AppLanguage.Spanish);
        return new Dictionary<AppLanguage, IReadOnlyDictionary<string, string>>
        {
            [AppLanguage.English] = english,
            [AppLanguage.Spanish] = spanish,
        };
    }
}
