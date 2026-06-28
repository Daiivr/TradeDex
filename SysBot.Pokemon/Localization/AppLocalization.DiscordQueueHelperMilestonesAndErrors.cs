using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordQueueHelperMilestonesAndErrorsTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: QueueHelper milestones and errors
                target[LocalizationKeys.DiscordMilestoneMedalTitle] = "{0}'s Milestone Medal";
                target[LocalizationKeys.DiscordMilestoneFirst] = "Congratulations on your first trade!\n**Status:** Newbie Trainer.";
                target[LocalizationKeys.DiscordMilestone50] = "You've reached 50 trades!\n**Status:** Novice Trainer.";
                target[LocalizationKeys.DiscordMilestone100] = "You've reached 100 trades!\n**Status:** Pokemon Professor.";
                target[LocalizationKeys.DiscordMilestone150] = "You've reached 150 trades!\n**Status:** Pokemon Specialist.";
                target[LocalizationKeys.DiscordMilestone200] = "You've reached 200 trades!\n**Status:** Pokemon Champion.";
                target[LocalizationKeys.DiscordMilestone250] = "You've reached 250 trades!\n**Status:** Pokemon Hero.";
                target[LocalizationKeys.DiscordMilestone300] = "You've reached 300 trades!\n**Status:** Pokemon Elite.";
                target[LocalizationKeys.DiscordMilestone350] = "You've reached 350 trades!\n**Status:** Pokemon Trader.";
                target[LocalizationKeys.DiscordMilestone400] = "You've reached 400 trades!\n**Status:** Pokemon Sage.";
                target[LocalizationKeys.DiscordMilestone450] = "You've reached 450 trades!\n**Status:** Pokemon Legend.";
                target[LocalizationKeys.DiscordMilestone500] = "You've reached 500 trades!\n**Status:** Region Master.";
                target[LocalizationKeys.DiscordMilestone550] = "You've reached 550 trades!\n**Status:** Trade Master.";
                target[LocalizationKeys.DiscordMilestone600] = "You've reached 600 trades!\n**Status:** World Famous.";
                target[LocalizationKeys.DiscordMilestone650] = "You've reached 650 trades!\n**Status:** Pokemon Master.";
                target[LocalizationKeys.DiscordMilestone700] = "You've reached 700 trades!\n**Status:** Pokemon God.";
                target[LocalizationKeys.DiscordMilestoneDefault] = "Congratulations on reaching {0} trades! Keep it going!";
                target[LocalizationKeys.DiscordMilestoneFirstTradeUnlock] = "Congratulations on your first trade! You unlocked a badge and a new title.";
                target[LocalizationKeys.DiscordMilestoneTradeUnlock] = "Congratulations on reaching {0} trades! You unlocked a badge and a new title.";
                target[LocalizationKeys.DiscordMilestoneCurrentSummary] = "This is your current milestone badge and trainer title.";
                target[LocalizationKeys.DiscordMilestoneTitleLabel] = "Title";
                target[LocalizationKeys.DiscordMilestoneBadgeLabel] = "Unlocked badge";
                target[LocalizationKeys.DiscordMilestoneTotalTradesLabel] = "Total trades";
                target[LocalizationKeys.DiscordMilestoneProgressTitle] = "Next badge progress";
                target[LocalizationKeys.DiscordMilestoneNextBadgeRemaining] = "Trades left for the next badge: **{0}**\nProgress: `{1}/{2}` trades";
                target[LocalizationKeys.DiscordMilestoneAllBadgesUnlocked] = "You have unlocked every available badge. That collection is complete!";
                target[LocalizationKeys.DiscordSendMessagesPermission] = "You must grant me \"Send Messages\" permissions!";
                target[LocalizationKeys.DiscordManageMessagesPermission] = "⚠️ <@{0}> You must grant me \"Manage Messages\" permissions!";
                target[LocalizationKeys.DiscordDmRequiredSelf] = "❌ You must enable private messages in order to be queued!";
                target[LocalizationKeys.DiscordDmRequiredMentioned] = "❌ The mentioned user must enable private messages in order for them to be queued!";
                target[LocalizationKeys.DiscordDiscordError] = "Discord error {0}: {1}";
                target[LocalizationKeys.DiscordHttpError] = "Http error {0}: {1}";
                break;
            case AppLanguage.Spanish:
                // Discord: QueueHelper milestones and errors
                target[LocalizationKeys.DiscordMilestoneMedalTitle] = "Medalla de hito de {0}";
                target[LocalizationKeys.DiscordMilestoneFirst] = "Felicidades por tu primer trade!\n**Estado:** Entrenador nuevo.";
                target[LocalizationKeys.DiscordMilestone50] = "Llegaste a 50 trades!\n**Estado:** Entrenador novato.";
                target[LocalizationKeys.DiscordMilestone100] = "Llegaste a 100 trades!\n**Estado:** Profesor Pokemon.";
                target[LocalizationKeys.DiscordMilestone150] = "Llegaste a 150 trades!\n**Estado:** Especialista Pokemon.";
                target[LocalizationKeys.DiscordMilestone200] = "Llegaste a 200 trades!\n**Estado:** Campeon Pokemon.";
                target[LocalizationKeys.DiscordMilestone250] = "Llegaste a 250 trades!\n**Estado:** Heroe Pokemon.";
                target[LocalizationKeys.DiscordMilestone300] = "Llegaste a 300 trades!\n**Estado:** Elite Pokemon.";
                target[LocalizationKeys.DiscordMilestone350] = "Llegaste a 350 trades!\n**Estado:** Trader Pokemon.";
                target[LocalizationKeys.DiscordMilestone400] = "Llegaste a 400 trades!\n**Estado:** Sabio Pokemon.";
                target[LocalizationKeys.DiscordMilestone450] = "Llegaste a 450 trades!\n**Estado:** Leyenda Pokemon.";
                target[LocalizationKeys.DiscordMilestone500] = "Llegaste a 500 trades!\n**Estado:** Maestro regional.";
                target[LocalizationKeys.DiscordMilestone550] = "Llegaste a 550 trades!\n**Estado:** Maestro de trades.";
                target[LocalizationKeys.DiscordMilestone600] = "Llegaste a 600 trades!\n**Estado:** Famoso mundial.";
                target[LocalizationKeys.DiscordMilestone650] = "Llegaste a 650 trades!\n**Estado:** Maestro Pokemon.";
                target[LocalizationKeys.DiscordMilestone700] = "Llegaste a 700 trades!\n**Estado:** Dios Pokemon.";
                target[LocalizationKeys.DiscordMilestoneDefault] = "Felicidades por llegar a {0} trades! Sigue asi!";
                target[LocalizationKeys.DiscordMilestoneFirstTradeUnlock] = "Felicidades por tu primer trade! Has desbloqueado una insignia y un nuevo titulo.";
                target[LocalizationKeys.DiscordMilestoneTradeUnlock] = "Felicidades por llegar a {0} trades! Has desbloqueado una insignia y un nuevo titulo.";
                target[LocalizationKeys.DiscordMilestoneCurrentSummary] = "Esta es tu insignia de hito actual y tu titulo de entrenador.";
                target[LocalizationKeys.DiscordMilestoneTitleLabel] = "Titulo";
                target[LocalizationKeys.DiscordMilestoneBadgeLabel] = "Insignia desbloqueada";
                target[LocalizationKeys.DiscordMilestoneTotalTradesLabel] = "Trades totales";
                target[LocalizationKeys.DiscordMilestoneProgressTitle] = "Progreso para la proxima insignia";
                target[LocalizationKeys.DiscordMilestoneNextBadgeRemaining] = "Trades restantes para la proxima insignia: **{0}**\nProgreso: `{1}/{2}` trades";
                target[LocalizationKeys.DiscordMilestoneAllBadgesUnlocked] = "Has desbloqueado todas las insignias disponibles. La coleccion esta completa!";
                target[LocalizationKeys.DiscordSendMessagesPermission] = "Debes darme permisos de \"Enviar mensajes\"!";
                target[LocalizationKeys.DiscordManageMessagesPermission] = "⚠️ <@{0}> Debes darme permisos de \"Gestionar mensajes\"!";
                target[LocalizationKeys.DiscordDmRequiredSelf] = "❌ Debes activar los mensajes privados para poder entrar en la cola!";
                target[LocalizationKeys.DiscordDmRequiredMentioned] = "❌ El usuario mencionado debe activar mensajes privados para entrar en la cola!";
                target[LocalizationKeys.DiscordDiscordError] = "Error de Discord {0}: {1}";
                target[LocalizationKeys.DiscordHttpError] = "Error HTTP {0}: {1}";
                break;
        }
    }
}
