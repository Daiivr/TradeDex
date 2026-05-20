using PKHeX.Core;
using SysBot.Pokemon.Localization;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon.Helpers;

/// <summary>
/// Simplified legality feedback that focuses on extracting data from LegalityAnalysis.Results
/// </summary>
public static class SimpleLegalityFeedback
{
    public static string GetLocalizedLegalityReport(LegalityAnalysis la)
    {
        if (AppLocalization.Language != AppLanguage.Spanish)
            return la.Report();

        var sb = new StringBuilder();
        var moves = la.Info.Moves;
        for (int i = 0; i < moves.Length; i++)
        {
            if (!moves[i].Valid)
                sb.AppendLine($"Movimiento invalido {i + 1}: Movimiento invalido.");
        }

        var invalidChecks = la.Results.Where(r => !r.Valid).ToList();
        if (invalidChecks.Count == 0)
            return sb.Length == 0 ? la.Report() : sb.ToString().TrimEnd();

        var localizationSet = LegalityLocalizationSet.GetLocalization("es");
        var context = LegalityLocalizationContext.Create(la, localizationSet);

        foreach (var check in invalidChecks)
            sb.AppendLine(context.Humanize(check));

        return sb.ToString().TrimEnd();
    }

    public static string GetLegalityReport(PKM pkm, LegalityAnalysis la, string speciesName)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"**Análisis de Legalidad para {speciesName}**");
        sb.AppendLine($"Estado: {(la.Valid ? "✅ Legal" : "❌ Ilegal")}");

        if (!la.Valid)
        {
            // Get all invalid checks from the Results list
            var invalidChecks = la.Results.Where(r => !r.Valid).ToList();

            if (invalidChecks.Count > 0)
            {
                sb.AppendLine("\n**Problemas Detectados:**");

                // Group by identifier for better organization
                var groupedIssues = invalidChecks.GroupBy(r => r.Identifier);

                // Create localization context to convert CheckResult to human-readable messages
                var localizationSet = LegalityLocalizationSet.GetLocalization(GameLanguage.DefaultLanguage);
                var context = LegalityLocalizationContext.Create(la, localizationSet);

                foreach (var group in groupedIssues)
                {
                    sb.AppendLine($"\n{GetCategoryIcon(group.Key)} **{GetCategoryName(group.Key)}:**");

                    foreach (var issue in group)
                    {
                        // Clean up the comment for display
                        var cleanComment = context.Humanize(issue)
                            .Replace("Invalid:", "")
                            .Replace("Fishy:", "Advertencia:")
                            .Trim();

                        sb.AppendLine($"  • {cleanComment}");
                    }
                }
            }

            // Add basic move analysis
            var moveIssues = invalidChecks.Where(r => r.Identifier == CheckIdentifier.CurrentMove).ToList();
            if (moveIssues.Count > 0)
            {
                sb.AppendLine("\n**Consejos sobre Movimientos:**");
                sb.AppendLine("  • Verifica si los movimientos están disponibles en la generación objetivo");
                sb.AppendLine("  • Comprueba que las combinaciones de movimientos sean legales entre sí");
                sb.AppendLine("  • Algunos movimientos son exclusivos de eventos");
            }
        }
        else
        {
            sb.AppendLine($"\n✨ ¡Tu {speciesName} pasó todas las verificaciones de legalidad!");
            if (la.EncounterOriginal != null)
            {
                sb.AppendLine($"Encuentro: {la.EncounterOriginal.LongName}");
            }
        }

        return sb.ToString();
    }

    public static string GetCategoryIcon(CheckIdentifier identifier) => identifier switch
    {
        CheckIdentifier.CurrentMove => "🎯",
        CheckIdentifier.Ability => "⚡",
        CheckIdentifier.Ball => "🏀",
        CheckIdentifier.Level => "📊",
        CheckIdentifier.Shiny => "✨",
        CheckIdentifier.Form => "🔄",
        CheckIdentifier.GameOrigin => "🎮",
        CheckIdentifier.Encounter => "📍",
        _ => "🔸"
    };

    public static string GetCategoryName(CheckIdentifier identifier) => identifier switch
    {
        CheckIdentifier.CurrentMove => "Moves",
        CheckIdentifier.RelearnMove => "Relearn Moves",
        CheckIdentifier.Ability => "Ability",
        CheckIdentifier.Ball => "Ball",
        CheckIdentifier.Level => "Level",
        CheckIdentifier.Shiny => "Shiny Status",
        CheckIdentifier.Form => "Form",
        CheckIdentifier.GameOrigin => "Game Origin",
        CheckIdentifier.Encounter => "Encounter",
        CheckIdentifier.IVs => "IVs",
        CheckIdentifier.EVs => "EVs",
        CheckIdentifier.Nature => "Nature",
        CheckIdentifier.Gender => "Gender",
        _ => identifier.ToString()
    };
}
