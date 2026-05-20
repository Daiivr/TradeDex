using PKHeX.Core;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon.Helpers;

/// <summary>
/// Provides detailed, user-friendly legality checking with specific error messages.
/// Focuses on extracting actionable feedback for common illegality issues.
/// ONLY used by slash commands - does not affect text-based trade commands.
/// </summary>
public static class DetailedLegalityChecker
{
    /// <summary>
    /// Performs a comprehensive legality check and returns specific, actionable error messages.
    /// </summary>
    /// <param name="pkm">The Pokemon to check</param>
    /// <param name="speciesName">Display name of the species</param>
    /// <param name="errorMessage">Detailed error message if illegal, null if legal</param>
    /// <returns>True if legal, false if illegal</returns>
    public static bool IsLegalWithDetailedReport(PKM pkm, string speciesName, string commandPrefix, out string? errorMessage)
    {
        var la = new LegalityAnalysis(pkm);

        if (la.Valid)
        {
            errorMessage = null;
            return true;
        }

        // Build detailed error message
        var sb = new StringBuilder();
        sb.AppendLine($"**{speciesName} is illegal in {GetGameName(pkm.Version)}**\n");

        // Get all invalid checks
        var invalidChecks = la.Results.Where(r => !r.Valid).ToList();

        if (invalidChecks.Count == 0)
        {
            // Fallback if no specific checks found
            sb.AppendLine("Esta configuración de Pokémon no es legal.");
            sb.AppendLine("\nPor favor intenta una combinación diferente de atributos.");
            errorMessage = sb.ToString();
            return false;
        }

        // Localization for readable messages
        var localizationSet = LegalityLocalizationSet.GetLocalization(GameLanguage.DefaultLanguage);
        var context = LegalityLocalizationContext.Create(la, localizationSet);

        // Check for specific common issues and provide targeted feedback
        bool hasShinyIssue = false;
        bool hasNatureIssue = false;
        bool hasFormIssue = false;
        bool hasEncounterIssue = false;

        // First pass: detect major issue types
        foreach (var check in invalidChecks)
        {
            var message = context.Humanize(check).ToLower();

            if (check.Identifier == CheckIdentifier.Shiny || message.Contains("shiny"))
                hasShinyIssue = true;
            if (check.Identifier == CheckIdentifier.Nature || message.Contains("nature"))
                hasNatureIssue = true;
            if (check.Identifier == CheckIdentifier.Form)
                hasFormIssue = true;
            if (check.Identifier == CheckIdentifier.Encounter)
                hasEncounterIssue = true;
        }

        // Provide specific, actionable feedback for major issues
        if (hasShinyIssue)
        {
            sb.AppendLine($"**❌ Problema con el Shiny:**");
            sb.AppendLine($"{speciesName} no puede ser shiny en {GetGameName(pkm.Version)}.");

            // Try to determine if it's shiny locked
            if (pkm.IsShiny && la.EncounterOriginal != null)
            {
                var enc = la.EncounterOriginal;
                if (enc is IFixedTrainer or MysteryGift)
                {
                    sb.AppendLine($"Este Pokémon proviene de un **encuentro shiny-locked**: {enc.LongName}");
                }
                else
                {
                    sb.AppendLine($"El shiny no está disponible para este encuentro específico.");
                }
            }

            sb.AppendLine($"\n**Solución:** Solicita a {speciesName} sin la opción shiny.\n");
        }

        if (hasNatureIssue && !hasShinyIssue) // Only show nature if it's the primary issue
        {
            sb.AppendLine($"**❌ Problema con la Naturaleza:**");

            // Try to get the forced nature from the encounter
            if (la.EncounterOriginal != null && TryGetForcedNature(la.EncounterOriginal, out Nature forcedNature))
            {
                sb.AppendLine($"{speciesName} debe tener **Naturaleza {forcedNature}** en {GetGameName(pkm.Version)}.");
                sb.AppendLine($"Naturaleza solicitada: **{pkm.Nature}**");
                sb.AppendLine($"\n**Solución:** Solicita a {speciesName} con Naturaleza {forcedNature}, o permite que el bot la elija automáticamente.\n");
            }
            else
            {
                sb.AppendLine($"La naturaleza **{pkm.Nature}** no es válida para este {speciesName}.");
                sb.AppendLine($"\n**Solución:** Prueba con una naturaleza diferente o permite que el bot la elija automáticamente.\n");
            }
        }

        if (hasFormIssue)
        {
            sb.AppendLine($"**❌ Problema con la Forma:**");
            sb.AppendLine($"Esta forma de {speciesName} no está disponible en {GetGameName(pkm.Version)}.");
            sb.AppendLine($"\n**Solución:** Selecciona una forma diferente o la forma base.\n");
        }

        // Add other specific issues
        if (!hasShinyIssue && !hasNatureIssue && !hasFormIssue)
        {
            sb.AppendLine("**Problemas Detectados:**\n");

            // Group by category
            var groupedIssues = invalidChecks.GroupBy(r => r.Identifier);

            foreach (var group in groupedIssues)
            {
                var categoryName = SimpleLegalityFeedback.GetCategoryName(group.Key);
                var icon = SimpleLegalityFeedback.GetCategoryIcon(group.Key);

                sb.AppendLine($"{icon} **{categoryName}:**");

                foreach (var issue in group.Take(3)) // Limit to 3 issues per category
                {
                    var cleanMessage = context.Humanize(issue)
                        .Replace("Invalid:", "")
                        .Replace("Fishy:", "")
                        .Trim();
                    sb.AppendLine($"  • {cleanMessage}");
                }

                if (group.Count() > 3)
                    sb.AppendLine($"  • ...y {group.Count() - 3} problema(s) más");

                sb.AppendLine();
            }
        }

        // Add encounter information if available
        if (hasEncounterIssue && la.EncounterOriginal != null)
        {
            sb.AppendLine($"**Tipo de Encuentro:** {la.EncounterOriginal.LongName}");
        }

        // Footer with helpful tip
        sb.AppendLine($"---");
        sb.AppendLine($"**Consejo:** Intenta usar el comando regular `{commandPrefix}trade` con un set de Showdown para tener más control,");
        sb.AppendLine($"o ajusta los atributos del Pokémon para que coincidan con lo que es legal en este juego.");

        errorMessage = sb.ToString();
        return false;
    }

    /// <summary>
    /// Attempts to determine if an encounter has a forced nature.
    /// </summary>
    private static bool TryGetForcedNature(IEncounterTemplate encounter, out Nature forcedNature)
    {
        forcedNature = Nature.Hardy;

        // Check for encounters with fixed nature
        if (encounter is IFixedNature fn && fn.Nature != Nature.Random)
        {
            forcedNature = fn.Nature;
            return true;
        }

        if (encounter is EncounterStatic9 or EncounterStatic8 or EncounterStatic8a)
        {
            // We can't reliably determine the nature without more context,
            // but we can figure there might be a restriction
            return false;
        }

        return false;
    }

    /// <summary>
    /// Gets game name from the version.
    /// </summary>
    private static string GetGameName(GameVersion version) => version switch
    {
        GameVersion.SL or GameVersion.VL or GameVersion.SV => "SV",
        GameVersion.PLA => "PLA",
        GameVersion.BD or GameVersion.SP or GameVersion.BDSP => "BDSP",
        GameVersion.SW or GameVersion.SH or GameVersion.SWSH => "SWSH",
        GameVersion.GP or GameVersion.GE => "LGPE",
        GameVersion.ZA => "PLZA",
        _ => version.ToString()
    };

    /// <summary>
    /// Quick legality check without detailed error message.
    /// </summary>
    public static bool IsLegal(PKM pkm)
    {
        var la = new LegalityAnalysis(pkm);
        return la.Valid;
    }
}
