using SysBot.Pokemon.Localization;

namespace SysBot.Pokemon;

public enum PokeTradeResult
{
    Success,

    // Trade Partner Failures
    NoTrainerFound,

    TrainerTooSlow,

    TrainerLeft,

    TrainerOfferCanceledQuick,

    TrainerRequestBad,

    IllegalTrade,

    SuspiciousActivity,

    UserCanceled,

    // Recovery -- General Bot Failures
    // Anything below here should be retried once if possible.
    RoutineCancel,

    ExceptionConnection,

    ExceptionInternal,

    RecoverStart,

    RecoverPostLinkCode,

    RecoverOpenBox,

    RecoverReturnOverworld,

    RecoverEnterUnionRoom,

    TradeEvolveNotAllowed,
}

public static class PokeTradeResultExtensions
{
    public static string GetDescription(this PokeTradeResult result)
    {
        var key = result switch
        {
            PokeTradeResult.Success => LocalizationKeys.DiscordTradeResultSuccess,
            PokeTradeResult.NoTrainerFound => LocalizationKeys.DiscordTradeResultNoTrainerFound,
            PokeTradeResult.TrainerTooSlow => LocalizationKeys.DiscordTradeResultTrainerTooSlow,
            PokeTradeResult.TrainerLeft => LocalizationKeys.DiscordTradeResultTrainerLeft,
            PokeTradeResult.TrainerOfferCanceledQuick => LocalizationKeys.DiscordTradeResultTrainerOfferCanceledQuick,
            PokeTradeResult.TrainerRequestBad => LocalizationKeys.DiscordTradeResultTrainerRequestBad,
            PokeTradeResult.IllegalTrade => LocalizationKeys.DiscordTradeResultIllegalTrade,
            PokeTradeResult.SuspiciousActivity => LocalizationKeys.DiscordTradeResultSuspiciousActivity,
            PokeTradeResult.UserCanceled => LocalizationKeys.DiscordTradeResultUserCanceled,
            PokeTradeResult.RoutineCancel => LocalizationKeys.DiscordTradeResultRoutineCancel,
            PokeTradeResult.ExceptionConnection => LocalizationKeys.DiscordTradeResultExceptionConnection,
            PokeTradeResult.ExceptionInternal => LocalizationKeys.DiscordTradeResultExceptionInternal,
            PokeTradeResult.RecoverStart => LocalizationKeys.DiscordTradeResultRecoverStart,
            PokeTradeResult.RecoverPostLinkCode => LocalizationKeys.DiscordTradeResultRecoverPostLinkCode,
            PokeTradeResult.RecoverOpenBox => LocalizationKeys.DiscordTradeResultRecoverOpenBox,
            PokeTradeResult.RecoverReturnOverworld => LocalizationKeys.DiscordTradeResultRecoverReturnOverworld,
            PokeTradeResult.RecoverEnterUnionRoom => LocalizationKeys.DiscordTradeResultRecoverEnterUnionRoom,
            PokeTradeResult.TradeEvolveNotAllowed => LocalizationKeys.DiscordTradeResultTradeEvolveNotAllowed,
            _ => string.Empty,
        };

        return string.IsNullOrEmpty(key)
            ? result.ToString()
            : $"__{AppLocalization.Get(key)}__";
    }

    public static bool ShouldAttemptRetry(this PokeTradeResult t) => t >= PokeTradeResult.RoutineCancel;
}
