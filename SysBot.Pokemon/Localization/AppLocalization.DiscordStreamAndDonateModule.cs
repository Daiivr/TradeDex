using System.Collections.Generic;

namespace SysBot.Pokemon.Localization;

public static partial class AppLocalization
{
    private static void AddDiscordStreamAndDonateModuleTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                target[LocalizationKeys.DiscordRequestedBy] = "Requested by {0}";
                target[LocalizationKeys.DiscordStreamTitle] = "🎥 {0} Stream 🎥";
                target[LocalizationKeys.DiscordStreamDescription] = "{0}\n\n[🔗 Click here to watch the stream]({1})";
                target[LocalizationKeys.DiscordStreamPlatformField] = "🌐 Platform";
                target[LocalizationKeys.DiscordStreamLinkField] = "📺 Link";
                target[LocalizationKeys.DiscordStreamLinkValue] = "[Click here]({0})";
                target[LocalizationKeys.DiscordStreamMessageOne] = "Check out the stream!";
                target[LocalizationKeys.DiscordStreamMessageTwo] = "Don't miss it!";
                target[LocalizationKeys.DiscordStreamMessageThree] = "Live stream is on now!";
                target[LocalizationKeys.DiscordStreamMessageFour] = "Join the fun!";
                target[LocalizationKeys.DiscordStreamMessageFive] = "Live right now!";
                target[LocalizationKeys.DiscordDonateNoLink] = "❌ No donation link is configured.";
                target[LocalizationKeys.DiscordDonateTitle] = "❤️ Donation Link! ❤️";
                target[LocalizationKeys.DiscordDonateDescription] = "{0}\n\n[Click here to donate]({1})";
                target[LocalizationKeys.DiscordDonateProgressField] = "Goal Progress";
                target[LocalizationKeys.DiscordDonateProgressValue] = "{0}\n**{1} / {2}** ({3:0}%)";
                target[LocalizationKeys.DiscordDonateRemainingField] = "Remaining";
                target[LocalizationKeys.DiscordDonateButton] = "Donate 💖";
                target[LocalizationKeys.DiscordDonateThanksOne] = "Thanks for your support!";
                target[LocalizationKeys.DiscordDonateThanksTwo] = "Your donation means a lot!";
                target[LocalizationKeys.DiscordDonateThanksThree] = "You're amazing for supporting us!";
                target[LocalizationKeys.DiscordDonateThanksFour] = "Thanks for being part of this!";
                target[LocalizationKeys.DiscordDonateThanksFive] = "Your generosity is appreciated!";
                break;
            case AppLanguage.Spanish:
                target[LocalizationKeys.DiscordRequestedBy] = "Solicitado por {0}";
                target[LocalizationKeys.DiscordStreamTitle] = "🎥 Stream de {0} 🎥";
                target[LocalizationKeys.DiscordStreamDescription] = "{0}\n\n[🔗 Haz clic aqui para ver el stream]({1})";
                target[LocalizationKeys.DiscordStreamPlatformField] = "🌐 Plataforma";
                target[LocalizationKeys.DiscordStreamLinkField] = "📺 Enlace";
                target[LocalizationKeys.DiscordStreamLinkValue] = "[Click aqui]({0})";
                target[LocalizationKeys.DiscordStreamMessageOne] = "Dale un vistazo al stream!";
                target[LocalizationKeys.DiscordStreamMessageTwo] = "No te lo pierdas!";
                target[LocalizationKeys.DiscordStreamMessageThree] = "Transmision en vivo ahora!";
                target[LocalizationKeys.DiscordStreamMessageFour] = "Unete a la diversion!";
                target[LocalizationKeys.DiscordStreamMessageFive] = "En vivo ahora mismo!";
                target[LocalizationKeys.DiscordDonateNoLink] = "❌ No hay un enlace de donacion configurado.";
                target[LocalizationKeys.DiscordDonateTitle] = "❤️ Enlace de Donacion! ❤️";
                target[LocalizationKeys.DiscordDonateDescription] = "{0}\n\n[Haz clic aqui para donar]({1})";
                target[LocalizationKeys.DiscordDonateProgressField] = "Progreso de la meta";
                target[LocalizationKeys.DiscordDonateProgressValue] = "{0}\n**{1} / {2}** ({3:0}%)";
                target[LocalizationKeys.DiscordDonateRemainingField] = "Restante";
                target[LocalizationKeys.DiscordDonateButton] = "Donar 💖";
                target[LocalizationKeys.DiscordDonateThanksOne] = "Gracias por tu apoyo!";
                target[LocalizationKeys.DiscordDonateThanksTwo] = "Tu donacion significa mucho!";
                target[LocalizationKeys.DiscordDonateThanksThree] = "Eres increible por apoyarnos!";
                target[LocalizationKeys.DiscordDonateThanksFour] = "Gracias por ser parte de esto!";
                target[LocalizationKeys.DiscordDonateThanksFive] = "Tu generosidad es apreciada!";
                break;
        }
    }
}
