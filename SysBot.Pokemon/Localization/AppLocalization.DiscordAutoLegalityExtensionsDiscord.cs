using System.Collections.Generic;
namespace SysBot.Pokemon.Localization;
public static partial class AppLocalization
{
    private static void AddDiscordAutoLegalityExtensionsDiscordTranslations(Dictionary<string, string> target, AppLanguage language)
    {
        switch (language)
        {
            case AppLanguage.English:
                // Discord: AutoLegalityExtensionsDiscord
                target[LocalizationKeys.DiscordUnableInterpretShowdown] = "Oops! I wasn't able to interpret your message! If you intended to convert something, please double check what you're pasting!";
                target[LocalizationKeys.DiscordFailedGenerateFromSet] = "Failed to generate Pokemon from your set.";
                target[LocalizationKeys.DiscordShowdownOopsReason] = "Oops! {0}";
                target[LocalizationKeys.DiscordDownloadPkmFailed] = "Failed to download PKM data: {0}";
                target[LocalizationKeys.DiscordLegalizationSuccessTitle] = "Legalization Successful";
                target[LocalizationKeys.DiscordLegalizationErrorTitle] = "Legalization Error";
                target[LocalizationKeys.DiscordLegalizationWarningTitle] = "Warning";
                target[LocalizationKeys.DiscordLegalizationDetailsLabel] = "Details";
                target[LocalizationKeys.DiscordLegalizationShowdownTextLabel] = "Showdown Text";
                target[LocalizationKeys.DiscordLegalizationSpeciesLabel] = "Species";
                target[LocalizationKeys.DiscordLegalizationEncounterTypeLabel] = "Encounter Type";
                target[LocalizationKeys.DiscordLegalizationResultLabel] = "Result";
                target[LocalizationKeys.DiscordLegalizationStatusLabel] = "Status";
                target[LocalizationKeys.DiscordLegalizationReasonLabel] = "Reason";
                target[LocalizationKeys.DiscordLegalizationInfoLabel] = "Information";
                target[LocalizationKeys.DiscordLegalizationSuccessfulStatus] = "Legalization successful.";
                target[LocalizationKeys.DiscordLegalizationFailedStatus] = "Failed to legalize.";
                target[LocalizationKeys.DiscordLegalizationCopyFooter] = "Copy the Regen template text between the ``` marks to use it.";
                target[LocalizationKeys.DiscordLegalizedEgg] = "Here's your legalized egg for **{1}**.";
                target[LocalizationKeys.DiscordLegalizedPkmShowdown] = "Here's your legalized **{1}**.";
                target[LocalizationKeys.DiscordUnexpectedShowdownProblem] = "Oops! An unexpected problem happened with this Showdown Set:\n```{0}```\nError: {1}";
                target[LocalizationKeys.DiscordAlreadyLegalFile] = "{0}: Already legal.";
                target[LocalizationKeys.DiscordUnableLegalizeFile] = "{0}: Unable to legalize.";
                target[LocalizationKeys.DiscordLegalizedPkmFile] = "Here's your legalized PKM for {0}!\n{1}";
                break;
            case AppLanguage.Spanish:
                // Discord: AutoLegalityExtensionsDiscord
                target[LocalizationKeys.DiscordUnableInterpretShowdown] = "Ups! No pude interpretar tu mensaje. Si querias convertir algo, revisa lo que pegaste!";
                target[LocalizationKeys.DiscordFailedGenerateFromSet] = "No se pudo generar el Pokemon desde tu set.";
                target[LocalizationKeys.DiscordShowdownOopsReason] = "Ups! {0}";
                target[LocalizationKeys.DiscordDownloadPkmFailed] = "No se pudieron descargar los datos PKM: {0}";
                target[LocalizationKeys.DiscordLegalizationSuccessTitle] = "Legalizacion exitosa";
                target[LocalizationKeys.DiscordLegalizationErrorTitle] = "Error de legalizacion";
                target[LocalizationKeys.DiscordLegalizationWarningTitle] = "Advertencia";
                target[LocalizationKeys.DiscordLegalizationDetailsLabel] = "Detalles";
                target[LocalizationKeys.DiscordLegalizationShowdownTextLabel] = "Showdown Text";
                target[LocalizationKeys.DiscordLegalizationSpeciesLabel] = "Especie";
                target[LocalizationKeys.DiscordLegalizationEncounterTypeLabel] = "Tipo de encuentro";
                target[LocalizationKeys.DiscordLegalizationResultLabel] = "Resultado";
                target[LocalizationKeys.DiscordLegalizationStatusLabel] = "Estado";
                target[LocalizationKeys.DiscordLegalizationReasonLabel] = "Razon";
                target[LocalizationKeys.DiscordLegalizationInfoLabel] = "Informacion";
                target[LocalizationKeys.DiscordLegalizationSuccessfulStatus] = "Legalizacion exitosa.";
                target[LocalizationKeys.DiscordLegalizationFailedStatus] = "Fallo al legalizar.";
                target[LocalizationKeys.DiscordLegalizationCopyFooter] = "Copia el texto de la plantilla Regen entre las marcas ``` para usarlo.";
                target[LocalizationKeys.DiscordLegalizedEgg] = "Aqui esta tu huevo legalizado de **{1}**.";
                target[LocalizationKeys.DiscordLegalizedPkmShowdown] = "Aqui esta tu **{1}** legalizado.";
                target[LocalizationKeys.DiscordUnexpectedShowdownProblem] = "Ups! Ocurrio un problema inesperado con este Showdown Set:\n```{0}```\nError: {1}";
                target[LocalizationKeys.DiscordAlreadyLegalFile] = "{0}: ya es legal.";
                target[LocalizationKeys.DiscordUnableLegalizeFile] = "{0}: no se pudo legalizar.";
                target[LocalizationKeys.DiscordLegalizedPkmFile] = "Aqui esta tu PKM legalizado para {0}!\n{1}";
                break;
        }
    }
}
